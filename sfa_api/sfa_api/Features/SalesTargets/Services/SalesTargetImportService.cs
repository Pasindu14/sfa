using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using sfa_api.Features.SalesTargets.DTOs;
using sfa_api.Features.SalesTargets.Entities;
using sfa_api.Features.SalesTargets.Enums;
using sfa_api.Features.SalesTargets.Repositories;
using sfa_api.Features.SalesTargets.Requests;
using sfa_api.Features.Distributors.Repositories;
using sfa_api.Features.UserGeoAssignments.Repositories;
using sfa_api.Features.UserReportingLines.Repositories;
using sfa_api.Features.Users.Entities;
using sfa_api.Infrastructure.Persistence;

namespace sfa_api.Features.SalesTargets.Services;

public class SalesTargetImportService(
    ISalesTargetRepository targetRepo,
    ISalesTargetImportBatchRepository batchRepo,
    IUserReportingLineRepository reportingLineRepo,
    IUserGeoAssignmentRepository geoRepo,
    IDistributorRepository distributorRepo,
    AppDbContext context) : ISalesTargetImportService
{
    public async Task<ImportSalesTargetsResultDto> ImportAsync(
        ImportSalesTargetsRequest request,
        int callerId,
        CancellationToken ct = default)
    {
        var now  = DateTime.UtcNow;
        var seq  = await batchRepo.GetNextBatchNumberAsync(ct);
        var batchNumber = $"STG-{request.Year}-{seq:00000}";

        // ① Create batch record immediately so we have a BatchId
        var batch = new SalesTargetImportBatch
        {
            BatchNumber = batchNumber,
            FileName    = request.FileName,
            Year        = request.Year,
            Month       = request.Month,
            TotalRows   = request.Rows.Count,
            Status      = SalesTargetImportBatchStatus.Processing,
            ImportedBy  = callerId,
            ImportedAt  = now,
            CreatedAt   = now,
            UpdatedAt   = now,
        };
        await batchRepo.CreateAsync(batch, ct);
        await batchRepo.SaveChangesAsync(ct);

        // ② Collect distinct lookup sets
        var repIds    = request.Rows.Select(r => r.RepsCode).Distinct().ToList();
        var itemCodes = request.Rows.Select(r => r.ItemCode.Trim()).Distinct().ToList();

        // ③ PRE-FETCH everything — 5 batched queries, independent of row count
        var usersById = await context.Users
            .AsNoTracking()
            .Where(u => repIds.Contains(u.Id) && !u.IsDeleted)
            .ToDictionaryAsync(u => u.Id, ct);

        var productsByCode = await context.Products
            .AsNoTracking()
            .Where(p => itemCodes.Contains(p.Code) && p.IsActive && !p.IsDeleted)
            .ToDictionaryAsync(p => p.Code, ct);

        // Reporting lines — fetch all org levels needed (reps + their managers up to 4 hops)
        // We do one fetch of ALL active lines for ALL rep IDs, then walk in memory
        var reportingLines = await reportingLineRepo.GetActiveLinesForUsersAsync(repIds, ct);

        // Walk 4 hops in memory to collect all manager IDs (for lines we may need to fetch)
        var allManagerIds = new HashSet<int>();
        foreach (var repId in repIds)
        {
            if (reportingLines.TryGetValue(repId, out var supId)) { allManagerIds.Add(supId);
                if (reportingLines.TryGetValue(supId, out var asmId)) { allManagerIds.Add(asmId);
                    if (reportingLines.TryGetValue(asmId, out var rsmId)) { allManagerIds.Add(rsmId);
                        if (reportingLines.TryGetValue(rsmId, out var nsmId)) allManagerIds.Add(nsmId); } } }
        }
        // Fetch lines for managers too (to walk beyond level 1)
        var managerLines = await reportingLineRepo.GetActiveLinesForUsersAsync(allManagerIds, ct);
        foreach (var kv in managerLines)
            reportingLines.TryAdd(kv.Key, kv.Value);

        var geoByUserId = await context.UserGeoAssignments
            .AsNoTracking()
            .Where(g => repIds.Contains(g.UserId) && g.IsActive && !g.IsDeleted)
            .ToDictionaryAsync(g => g.UserId, ct);

        // Batch-resolve DistributorId via territory: one IN query, never per-row
        var territoryIds = geoByUserId.Values
            .Where(g => g.TerritoryId.HasValue)
            .Select(g => g.TerritoryId!.Value)
            .Distinct()
            .ToList();
        var distributorIdByTerritoryId = territoryIds.Count > 0
            ? await distributorRepo.GetDistributorIdsByTerritoryIdsAsync(territoryIds, ct)
            : new Dictionary<int, int>();

        // Resolve product IDs for the existing-target query
        var resolvedProductIds = request.Rows
            .Where(r => productsByCode.ContainsKey(r.ItemCode.Trim()))
            .Select(r => productsByCode[r.ItemCode.Trim()].Id)
            .Distinct()
            .ToList();

        var existingTargets = await targetRepo.GetExistingForMonthAsync(
            request.Year, request.Month, repIds, resolvedProductIds, ct);

        // ④ Walk rows in memory — no DB calls inside this loop
        var toInsert = new List<SalesTarget>();
        var toUpdate = new List<SalesTarget>();
        var errors   = new List<SalesTargetImportErrorDto>();
        int inserted = 0, updated = 0, skipped = 0;

        foreach (var row in request.Rows)
        {
            var itemCode = row.ItemCode.Trim();

            if (!usersById.TryGetValue(row.RepsCode, out var user))
            {
                errors.Add(new SalesTargetImportErrorDto(row.RowIndex, row.RepsCode, itemCode, $"Rep code {row.RepsCode} not found"));
                skipped++;
                continue;
            }

            if (user.Role != UserRole.SalesRep)
            {
                errors.Add(new SalesTargetImportErrorDto(row.RowIndex, row.RepsCode, itemCode, $"User {row.RepsCode} is not a SalesRep"));
                skipped++;
                continue;
            }

            if (!productsByCode.TryGetValue(itemCode, out var product))
            {
                errors.Add(new SalesTargetImportErrorDto(row.RowIndex, row.RepsCode, itemCode, $"Product code '{itemCode}' not found"));
                skipped++;
                continue;
            }

            // Walk org chain in-memory (4 hops, O(1) each)
            reportingLines.TryGetValue(row.RepsCode, out var supervisorId);
            int? supId = supervisorId == 0 ? null : supervisorId;
            int? asmId = null, rsmId = null, nsmId = null;
            if (supId.HasValue && reportingLines.TryGetValue(supId.Value, out var a) && a != 0) { asmId = a;
                if (reportingLines.TryGetValue(asmId.Value, out var r) && r != 0) { rsmId = r;
                    if (reportingLines.TryGetValue(rsmId.Value, out var n) && n != 0) nsmId = n; } }

            // Geo chain from UserGeoAssignment
            geoByUserId.TryGetValue(row.RepsCode, out var geo);
            int? distributorId = geo?.TerritoryId.HasValue == true
                && distributorIdByTerritoryId.TryGetValue(geo.TerritoryId!.Value, out var distId)
                ? distId : null;

            var key = (row.RepsCode, product.Id);

            if (existingTargets.TryGetValue(key, out var existing))
            {
                existing.TargetQuantity   = row.TargetQty;
                existing.ImportBatchId    = batch.Id;
                existing.SupervisorUserId = supId;
                existing.AsmUserId        = asmId;
                existing.RsmUserId        = rsmId;
                existing.NsmUserId        = nsmId;
                existing.DistributorId    = distributorId;
                existing.DivisionId       = geo?.DivisionId;
                existing.TerritoryId      = geo?.TerritoryId;
                existing.AreaId           = geo?.AreaId;
                existing.RegionId         = geo?.RegionId;
                existing.UpdatedAt        = now;
                existing.UpdatedBy        = callerId;
                toUpdate.Add(existing);
                updated++;
            }
            else
            {
                toInsert.Add(new SalesTarget
                {
                    ImportBatchId    = batch.Id,
                    Year             = request.Year,
                    Month            = request.Month,
                    SalesRepId       = row.RepsCode,
                    ProductId        = product.Id,
                    TargetQuantity   = row.TargetQty,
                    SupervisorUserId = supId,
                    AsmUserId        = asmId,
                    RsmUserId        = rsmId,
                    NsmUserId        = nsmId,
                    DistributorId    = distributorId,
                    DivisionId       = geo?.DivisionId,
                    TerritoryId      = geo?.TerritoryId,
                    AreaId           = geo?.AreaId,
                    RegionId         = geo?.RegionId,
                    CreatedAt        = now,
                    UpdatedAt        = now,
                    CreatedBy        = callerId,
                    UpdatedBy        = callerId,
                });
                inserted++;
            }
        }

        // ⑤ Single SaveChangesAsync — EF batches the statements
        if (toInsert.Count > 0) targetRepo.AddRange(toInsert);
        if (toUpdate.Count > 0) targetRepo.UpdateRange(toUpdate);
        await targetRepo.SaveChangesAsync(ct);

        // ⑥ Update batch row with final counters + status
        var status = skipped == 0
            ? SalesTargetImportBatchStatus.Completed
            : inserted + updated == 0
                ? SalesTargetImportBatchStatus.Failed
                : SalesTargetImportBatchStatus.PartialFailed;

        batch.InsertedRows  = inserted;
        batch.UpdatedRows   = updated;
        batch.SkippedRows   = skipped;
        batch.Status        = status;
        batch.ErrorSummary  = errors.Count > 0 ? JsonSerializer.Serialize(errors) : null;
        batch.UpdatedAt     = now;
        await batchRepo.SaveChangesAsync(ct);

        return new ImportSalesTargetsResultDto(
            BatchId:      batch.Id,
            BatchNumber:  batch.BatchNumber,
            Year:         request.Year,
            Month:        request.Month,
            TotalRows:    request.Rows.Count,
            InsertedRows: inserted,
            UpdatedRows:  updated,
            SkippedRows:  skipped,
            Status:       status,
            Errors:       errors);
    }
}
