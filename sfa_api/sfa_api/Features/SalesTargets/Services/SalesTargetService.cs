using sfa_api.Features.SalesTargets.DTOs;
using sfa_api.Features.SalesTargets.Repositories;

namespace sfa_api.Features.SalesTargets.Services;

public class SalesTargetService(
    ISalesTargetRepository targetRepo,
    ISalesTargetImportBatchRepository batchRepo) : ISalesTargetService
{
    public async Task<(IEnumerable<SalesTargetDto> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize,
        int? year = null,
        int? month = null,
        int? salesRepId = null,
        int? productId = null,
        string? search = null,
        CancellationToken ct = default)
    {
        pageSize = Math.Clamp(pageSize, 1, 200);
        var skip = (page - 1) * pageSize;

        var (items, total) = await targetRepo.GetPagedAsync(
            skip, pageSize, year, month, salesRepId, productId, search, ct);

        var dtos = items.Select(t => new SalesTargetDto(
            Id:               t.Id,
            ImportBatchId:    t.ImportBatchId,
            Year:             t.Year,
            Month:            t.Month,
            SalesRepId:       t.SalesRepId,
            SalesRepName:     t.SalesRep?.Name ?? string.Empty,
            ProductId:        t.ProductId,
            ProductCode:      t.Product?.Code ?? string.Empty,
            ProductName:      t.Product?.ItemDescription ?? string.Empty,
            TargetQuantity:   t.TargetQuantity,
            SupervisorUserId: t.SupervisorUserId,
            SupervisorName:   t.Supervisor?.Name,
            AsmUserId:        t.AsmUserId,
            RsmUserId:        t.RsmUserId,
            NsmUserId:        t.NsmUserId,
            DistributorId:    t.DistributorId,
            DivisionId:       t.DivisionId,
            TerritoryId:      t.TerritoryId,
            AreaId:           t.AreaId,
            RegionId:         t.RegionId,
            UpdatedAt:        t.UpdatedAt));

        return (dtos, total);
    }

    public async Task<(IEnumerable<SalesTargetImportBatchDto> Items, int TotalCount)> GetBatchesPagedAsync(
        int page, int pageSize, CancellationToken ct = default)
    {
        pageSize = Math.Clamp(pageSize, 1, 200);
        var skip = (page - 1) * pageSize;

        var (items, total) = await batchRepo.GetPagedAsync(skip, pageSize, ct);

        var dtos = items.Select(b => new SalesTargetImportBatchDto(
            Id:             b.Id,
            BatchNumber:    b.BatchNumber,
            FileName:       b.FileName,
            Year:           b.Year,
            Month:          b.Month,
            TotalRows:      b.TotalRows,
            InsertedRows:   b.InsertedRows,
            UpdatedRows:    b.UpdatedRows,
            SkippedRows:    b.SkippedRows,
            Status:         b.Status,
            ImportedBy:     b.ImportedBy,
            ImportedByName: b.Importer?.Name ?? string.Empty,
            ImportedAt:     b.ImportedAt));

        return (dtos, total);
    }

    public async Task<SalesTargetImportBatchDto?> GetBatchByIdAsync(int id, CancellationToken ct = default)
    {
        var b = await batchRepo.GetByIdAsync(id, ct);
        if (b is null) return null;

        return new SalesTargetImportBatchDto(
            Id:             b.Id,
            BatchNumber:    b.BatchNumber,
            FileName:       b.FileName,
            Year:           b.Year,
            Month:          b.Month,
            TotalRows:      b.TotalRows,
            InsertedRows:   b.InsertedRows,
            UpdatedRows:    b.UpdatedRows,
            SkippedRows:    b.SkippedRows,
            Status:         b.Status,
            ImportedBy:     b.ImportedBy,
            ImportedByName: b.Importer?.Name ?? string.Empty,
            ImportedAt:     b.ImportedAt);
    }
}
