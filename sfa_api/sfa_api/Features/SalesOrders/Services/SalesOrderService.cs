using System.Text.Json;
using sfa_api.Common.Errors;
using sfa_api.Features.SalesOrders.DTOs;
using sfa_api.Features.SalesOrders.Entities;
using sfa_api.Features.SalesOrders.Enums;
using sfa_api.Features.SalesOrders.Repositories;
using sfa_api.Features.SalesOrders.Requests;
using sfa_api.Features.Users.Entities;
using sfa_api.Features.Users.Repositories;

namespace sfa_api.Features.SalesOrders.Services;

public class SalesOrderService(
    ISalesOrderRepository repo,
    IUserRepository userRepo,
    ILogger<SalesOrderService> logger) : ISalesOrderService
{
    private readonly ISalesOrderRepository _repo = repo;
    private readonly IUserRepository _userRepo = userRepo;
    private readonly ILogger<SalesOrderService> _logger = logger;

    public async Task<SalesOrderDto> GetByIdAsync(int id, int callerId, UserRole callerRole, CancellationToken ct = default)
    {
        var order = await _repo.GetByIdWithItemsAsync(id, ct)
            ?? throw new NotFoundException("SalesOrder", id);

        // Distributors may only view their own orders
        if (callerRole == UserRole.Distributor)
        {
            var caller = await _userRepo.GetUserByIdAsync(callerId, ct)
                ?? throw new NotFoundException("User", callerId);
            if (order.DistributorId != caller.DistributorId)
                throw new AuthorizationException("this sales order");
        }

        // Load history and resolve performer names
        var history = await _repo.GetHistoryAsync(id, ct);
        var performerIds = history.Select(h => h.PerformedBy).Distinct().ToList();
        var performers = new Dictionary<int, string?>();
        foreach (var pid in performerIds)
        {
            var user = await _userRepo.GetUserByIdAsync(pid, ct);
            performers[pid] = user?.Name;
        }

        return MapToDto(order, history, performers);
    }

    public async Task<SalesOrderListDto> GetAllAsync(
        int page,
        int pageSize,
        string? search,
        SalesOrderStatus? status,
        DateTime? fromDate,
        DateTime? toDate,
        int callerId,
        UserRole callerRole,
        CancellationToken ct = default)
    {
        var skip = (page - 1) * pageSize;

        int? distributorFilter = null;
        if (callerRole == UserRole.Distributor)
        {
            var caller = await _userRepo.GetUserByIdAsync(callerId, ct)
                ?? throw new NotFoundException("User", callerId);
            distributorFilter = caller.DistributorId;
        }

        var (orders, totalCount) = await _repo.GetAllAsync(
            skip, pageSize, search, status, distributorFilter, fromDate, toDate, ct);

        return new SalesOrderListDto(
            SalesOrders: orders.Select(MapToSummaryDto),
            TotalCount: totalCount,
            Page: page,
            PageSize: pageSize
        );
    }

    public async Task<SalesOrderDto> CreateAsync(CreateSalesOrderRequest request, int callerId, UserRole callerRole, CancellationToken ct = default)
    {
        int distributorId;

        if (callerRole == UserRole.Admin)
        {
            if (!request.DistributorId.HasValue)
                throw new Common.Errors.ValidationException(new Dictionary<string, string[]>
                {
                    ["distributorId"] = ["DistributorId is required for Admin."]
                });
            distributorId = request.DistributorId.Value;
        }
        else
        {
            // Distributor role — resolve from their user record
            var caller = await _userRepo.GetUserByIdAsync(callerId, ct)
                ?? throw new NotFoundException("User", callerId);
            if (!caller.DistributorId.HasValue)
                throw new AuthorizationException("sales orders (no distributor assigned)");
            distributorId = caller.DistributorId.Value;
        }

        var seq = await _repo.GetNextOrderNumberAsync(ct);
        var orderNumber = $"SO-{DateTime.UtcNow.Year}-{seq:D5}";

        var order = new SalesOrder
        {
            OrderNumber = orderNumber,
            DistributorId = distributorId,
            Status = SalesOrderStatus.Draft,
            Notes = request.Notes,
            IsActive = true,
            CreatedBy = callerId,
            UpdatedBy = callerId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _repo.CreateAsync(order, ct);
        await _repo.SaveChangesAsync(ct);

        // Add items
        var items = request.Items.Select(i => new SalesOrderItem
        {
            SalesOrderId = order.Id,
            ProductId = i.ProductId,
            Quantity = i.Quantity,
            UnitPrice = i.UnitPrice,
            Discount = i.Discount
        }).ToList();

        await _repo.AddItemsAsync(items, ct);

        // Record history
        var snapshot = JsonSerializer.Serialize(request.Items);
        await _repo.AddHistoryAsync(new SalesOrderHistory
        {
            SalesOrderId = order.Id,
            Action = "Created",
            FromStatus = null,
            ToStatus = SalesOrderStatus.Draft,
            PerformedBy = callerId,
            PerformedAt = DateTime.UtcNow,
            Notes = request.Notes,
            ItemsSnapshot = snapshot
        }, ct);

        await _repo.SaveChangesAsync(ct);

        _logger.LogInformation("SalesOrder {OrderNumber} created by user {CallerId}", orderNumber, callerId);

        var created = await _repo.GetByIdWithItemsAsync(order.Id, ct);
        return MapToDto(created!);
    }

    public async Task<SalesOrderDto> UpdateAsync(int id, UpdateSalesOrderRequest request, int callerId, UserRole callerRole, CancellationToken ct = default)
    {
        var order = await _repo.GetByIdWithItemsAsync(id, ct)
            ?? throw new NotFoundException("SalesOrder", id);

        var allowedStatuses = callerRole switch
        {
            UserRole.Distributor => new[] { SalesOrderStatus.Draft },
            UserRole.SalesRep    => new[] { SalesOrderStatus.PendingRepApproval },
            UserRole.Manager     => new[] { SalesOrderStatus.PendingManagerApproval },
            UserRole.Admin       => new[] { SalesOrderStatus.Draft, SalesOrderStatus.PendingRepApproval, SalesOrderStatus.PendingManagerApproval },
            _                    => Array.Empty<SalesOrderStatus>()
        };

        if (!allowedStatuses.Contains(order.Status))
            throw new BusinessRuleException("ORDER_NOT_EDITABLE", "Order cannot be edited at this stage.");

        // Distributors may only update their own orders
        if (callerRole == UserRole.Distributor)
        {
            var caller = await _userRepo.GetUserByIdAsync(callerId, ct)
                ?? throw new NotFoundException("User", callerId);
            if (order.DistributorId != caller.DistributorId)
                throw new AuthorizationException("this sales order");
        }

        // Snapshot before replacing items
        var beforeSnapshot = JsonSerializer.Serialize(order.Items.Select(i => new
        {
            i.ProductId,
            i.Quantity,
            i.UnitPrice,
            i.Discount
        }));

        order.Notes = request.Notes;
        order.UpdatedBy = callerId;
        order.UpdatedAt = DateTime.UtcNow;

        await _repo.UpdateAsync(order, ct);

        // Replace all items
        await _repo.RemoveItemsAsync(id, ct);
        var newItems = request.Items.Select(i => new SalesOrderItem
        {
            SalesOrderId = id,
            ProductId = i.ProductId,
            Quantity = i.Quantity,
            UnitPrice = i.UnitPrice,
            Discount = i.Discount
        }).ToList();
        await _repo.AddItemsAsync(newItems, ct);

        // Record history
        var afterSnapshot = JsonSerializer.Serialize(request.Items);
        await _repo.AddHistoryAsync(new SalesOrderHistory
        {
            SalesOrderId = id,
            Action = "ItemsEdited",
            FromStatus = order.Status,
            ToStatus = order.Status,
            PerformedBy = callerId,
            PerformedAt = DateTime.UtcNow,
            Notes = $"Before: {beforeSnapshot}",
            ItemsSnapshot = afterSnapshot
        }, ct);

        await _repo.SaveChangesAsync(ct);

        _logger.LogInformation("SalesOrder {OrderId} updated by user {CallerId}", id, callerId);

        var updated = await _repo.GetByIdWithItemsAsync(id, ct);
        return MapToDto(updated!);
    }

    public async Task<SalesOrderDto> SubmitAsync(int id, int callerId, UserRole callerRole, CancellationToken ct = default)
    {
        if (callerRole != UserRole.Distributor && callerRole != UserRole.Admin)
            throw new AuthorizationException("submit sales orders");

        var order = await _repo.GetByIdWithItemsAsync(id, ct)
            ?? throw new NotFoundException("SalesOrder", id);

        if (order.Status != SalesOrderStatus.Draft)
            throw new BusinessRuleException("ORDER_NOT_SUBMITTABLE", "Only Draft orders can be submitted.");

        if (callerRole == UserRole.Distributor)
        {
            var caller = await _userRepo.GetUserByIdAsync(callerId, ct)
                ?? throw new NotFoundException("User", callerId);
            if (order.DistributorId != caller.DistributorId)
                throw new AuthorizationException("this sales order");
        }

        var fromStatus = order.Status;
        order.Status = SalesOrderStatus.PendingRepApproval;
        order.SubmittedBy = callerId;
        order.SubmittedAt = DateTime.UtcNow;
        order.UpdatedBy = callerId;
        order.UpdatedAt = DateTime.UtcNow;

        await _repo.UpdateAsync(order, ct);
        await _repo.AddHistoryAsync(new SalesOrderHistory
        {
            SalesOrderId = id,
            Action = "Submitted",
            FromStatus = fromStatus,
            ToStatus = SalesOrderStatus.PendingRepApproval,
            PerformedBy = callerId,
            PerformedAt = DateTime.UtcNow
        }, ct);

        await _repo.SaveChangesAsync(ct);

        _logger.LogInformation("SalesOrder {OrderId} submitted by user {CallerId}", id, callerId);

        var updated = await _repo.GetByIdWithItemsAsync(id, ct);
        return MapToDto(updated!);
    }

    public async Task<SalesOrderDto> RepApproveAsync(int id, int callerId, UserRole callerRole, CancellationToken ct = default)
    {
        if (callerRole != UserRole.SalesRep && callerRole != UserRole.Admin)
            throw new AuthorizationException("rep-approve sales orders");

        var order = await _repo.GetByIdWithItemsAsync(id, ct)
            ?? throw new NotFoundException("SalesOrder", id);

        if (order.Status != SalesOrderStatus.PendingRepApproval)
            throw new BusinessRuleException("ORDER_NOT_PENDING_REP_APPROVAL", "Order is not in PendingRepApproval status.");

        var fromStatus = order.Status;
        order.Status = SalesOrderStatus.PendingManagerApproval;
        order.RepApprovedBy = callerId;
        order.RepApprovedAt = DateTime.UtcNow;
        order.UpdatedBy = callerId;
        order.UpdatedAt = DateTime.UtcNow;

        await _repo.UpdateAsync(order, ct);
        await _repo.AddHistoryAsync(new SalesOrderHistory
        {
            SalesOrderId = id,
            Action = "RepApproved",
            FromStatus = fromStatus,
            ToStatus = SalesOrderStatus.PendingManagerApproval,
            PerformedBy = callerId,
            PerformedAt = DateTime.UtcNow
        }, ct);

        await _repo.SaveChangesAsync(ct);

        _logger.LogInformation("SalesOrder {OrderId} rep-approved by user {CallerId}", id, callerId);

        var updated = await _repo.GetByIdWithItemsAsync(id, ct);
        return MapToDto(updated!);
    }

    public async Task<SalesOrderDto> ApproveAsync(int id, int callerId, UserRole callerRole, CancellationToken ct = default)
    {
        if (callerRole != UserRole.Manager && callerRole != UserRole.Admin)
            throw new AuthorizationException("approve sales orders");

        var order = await _repo.GetByIdWithItemsAsync(id, ct)
            ?? throw new NotFoundException("SalesOrder", id);

        if (order.Status != SalesOrderStatus.PendingManagerApproval)
            throw new BusinessRuleException("ORDER_NOT_PENDING_MANAGER_APPROVAL", "Order is not in PendingManagerApproval status.");

        var fromStatus = order.Status;
        order.Status = SalesOrderStatus.PendingDistributorFinalization;
        order.ManagerApprovedBy = callerId;
        order.ManagerApprovedAt = DateTime.UtcNow;
        order.UpdatedBy = callerId;
        order.UpdatedAt = DateTime.UtcNow;

        await _repo.UpdateAsync(order, ct);
        await _repo.AddHistoryAsync(new SalesOrderHistory
        {
            SalesOrderId = id,
            Action = "ManagerApproved",
            FromStatus = fromStatus,
            ToStatus = SalesOrderStatus.PendingDistributorFinalization,
            PerformedBy = callerId,
            PerformedAt = DateTime.UtcNow
        }, ct);

        await _repo.SaveChangesAsync(ct);

        _logger.LogInformation("SalesOrder {OrderId} manager-approved by user {CallerId}", id, callerId);

        var updated = await _repo.GetByIdWithItemsAsync(id, ct);
        return MapToDto(updated!);
    }

    public async Task<SalesOrderDto> RejectAsync(int id, RejectSalesOrderRequest request, int callerId, UserRole callerRole, CancellationToken ct = default)
    {
        var order = await _repo.GetByIdWithItemsAsync(id, ct)
            ?? throw new NotFoundException("SalesOrder", id);

        // Role + status gate
        if (order.Status == SalesOrderStatus.PendingRepApproval
            && callerRole != UserRole.SalesRep && callerRole != UserRole.Admin)
            throw new AuthorizationException("reject orders at this stage (SalesRep or Admin only)");

        if (order.Status == SalesOrderStatus.PendingManagerApproval
            && callerRole != UserRole.Manager && callerRole != UserRole.Admin)
            throw new AuthorizationException("reject orders at this stage (Manager or Admin only)");

        if (order.Status != SalesOrderStatus.PendingRepApproval
            && order.Status != SalesOrderStatus.PendingManagerApproval)
            throw new BusinessRuleException("ORDER_NOT_REJECTABLE", "Order cannot be rejected at this stage.");

        var fromStatus = order.Status;
        order.Status = SalesOrderStatus.PendingDistributorAcknowledgement;
        order.CancelReason = request.Reason;
        order.UpdatedBy = callerId;
        order.UpdatedAt = DateTime.UtcNow;

        await _repo.UpdateAsync(order, ct);
        await _repo.AddHistoryAsync(new SalesOrderHistory
        {
            SalesOrderId = id,
            Action = "Rejected",
            FromStatus = fromStatus,
            ToStatus = SalesOrderStatus.PendingDistributorAcknowledgement,
            PerformedBy = callerId,
            PerformedAt = DateTime.UtcNow,
            Notes = request.Reason
        }, ct);

        await _repo.SaveChangesAsync(ct);

        _logger.LogInformation("SalesOrder {OrderId} rejected by user {CallerId}", id, callerId);

        var updated = await _repo.GetByIdWithItemsAsync(id, ct);
        return MapToDto(updated!);
    }

    public async Task<SalesOrderDto> AcknowledgeAsync(int id, int callerId, UserRole callerRole, CancellationToken ct = default)
    {
        if (callerRole != UserRole.Distributor && callerRole != UserRole.Admin)
            throw new AuthorizationException("acknowledge sales order rejections");

        var order = await _repo.GetByIdWithItemsAsync(id, ct)
            ?? throw new NotFoundException("SalesOrder", id);

        if (order.Status != SalesOrderStatus.PendingDistributorAcknowledgement)
            throw new BusinessRuleException("ORDER_NOT_PENDING_ACKNOWLEDGEMENT",
                "Order is not pending Distributor acknowledgement.");

        if (callerRole == UserRole.Distributor)
        {
            var caller = await _userRepo.GetUserByIdAsync(callerId, ct)
                ?? throw new NotFoundException("User", callerId);
            if (order.DistributorId != caller.DistributorId)
                throw new AuthorizationException("this sales order");
        }

        var fromStatus = order.Status;
        order.Status = SalesOrderStatus.Cancelled;
        order.AcknowledgedBy = callerId;
        order.AcknowledgedAt = DateTime.UtcNow;
        order.CancelledBy = callerId;
        order.CancelledAt = DateTime.UtcNow;
        order.UpdatedBy = callerId;
        order.UpdatedAt = DateTime.UtcNow;

        await _repo.UpdateAsync(order, ct);
        await _repo.AddHistoryAsync(new SalesOrderHistory
        {
            SalesOrderId = id,
            Action = "RejectionAcknowledged",
            FromStatus = fromStatus,
            ToStatus = SalesOrderStatus.Cancelled,
            PerformedBy = callerId,
            PerformedAt = DateTime.UtcNow
        }, ct);

        await _repo.SaveChangesAsync(ct);

        _logger.LogInformation("SalesOrder {OrderId} rejection acknowledged by user {CallerId}", id, callerId);

        var updated = await _repo.GetByIdWithItemsAsync(id, ct);
        return MapToDto(updated!);
    }

    public async Task<SalesOrderDto> FinalizeAsync(int id, int callerId, UserRole callerRole, CancellationToken ct = default)
    {
        if (callerRole != UserRole.Distributor && callerRole != UserRole.Admin)
            throw new AuthorizationException("finalize sales orders");

        var order = await _repo.GetByIdWithItemsAsync(id, ct)
            ?? throw new NotFoundException("SalesOrder", id);

        if (order.Status != SalesOrderStatus.PendingDistributorFinalization)
            throw new BusinessRuleException("ORDER_NOT_PENDING_FINALIZATION", "Order is not in PendingDistributorFinalization status.");

        if (callerRole == UserRole.Distributor)
        {
            var caller = await _userRepo.GetUserByIdAsync(callerId, ct)
                ?? throw new NotFoundException("User", callerId);
            if (order.DistributorId != caller.DistributorId)
                throw new AuthorizationException("this sales order");
        }

        var fromStatus = order.Status;
        order.Status = SalesOrderStatus.Finalized;
        order.FinalizedBy = callerId;
        order.FinalizedAt = DateTime.UtcNow;
        order.UpdatedBy = callerId;
        order.UpdatedAt = DateTime.UtcNow;

        await _repo.UpdateAsync(order, ct);
        await _repo.AddHistoryAsync(new SalesOrderHistory
        {
            SalesOrderId = id,
            Action = "Finalized",
            FromStatus = fromStatus,
            ToStatus = SalesOrderStatus.Finalized,
            PerformedBy = callerId,
            PerformedAt = DateTime.UtcNow
        }, ct);

        await _repo.SaveChangesAsync(ct);

        _logger.LogInformation("SalesOrder {OrderId} finalized by user {CallerId}", id, callerId);

        var updated = await _repo.GetByIdWithItemsAsync(id, ct);
        return MapToDto(updated!);
    }

    public async Task<SalesOrderDto> CancelAsync(int id, RejectSalesOrderRequest request, int callerId, UserRole callerRole, CancellationToken ct = default)
    {
        var order = await _repo.GetByIdWithItemsAsync(id, ct)
            ?? throw new NotFoundException("SalesOrder", id);

        // Distributors may only cancel Draft orders
        if (callerRole == UserRole.Distributor && order.Status != SalesOrderStatus.Draft)
            throw new BusinessRuleException("ORDER_NOT_CANCELLABLE", "Distributors can only cancel Draft orders.");

        if (callerRole != UserRole.Distributor && callerRole != UserRole.Admin)
            throw new AuthorizationException("cancel sales orders");

        if (order.Status == SalesOrderStatus.Finalized || order.Status == SalesOrderStatus.Cancelled)
            throw new BusinessRuleException("ORDER_NOT_CANCELLABLE", "Order is already finalized or cancelled.");

        if (callerRole == UserRole.Distributor)
        {
            var caller = await _userRepo.GetUserByIdAsync(callerId, ct)
                ?? throw new NotFoundException("User", callerId);
            if (order.DistributorId != caller.DistributorId)
                throw new AuthorizationException("this sales order");
        }

        var fromStatus = order.Status;
        order.Status = SalesOrderStatus.Cancelled;
        order.CancelledBy = callerId;
        order.CancelledAt = DateTime.UtcNow;
        order.CancelReason = request.Reason;
        order.UpdatedBy = callerId;
        order.UpdatedAt = DateTime.UtcNow;

        await _repo.UpdateAsync(order, ct);
        await _repo.AddHistoryAsync(new SalesOrderHistory
        {
            SalesOrderId = id,
            Action = "Cancelled",
            FromStatus = fromStatus,
            ToStatus = SalesOrderStatus.Cancelled,
            PerformedBy = callerId,
            PerformedAt = DateTime.UtcNow,
            Notes = request.Reason
        }, ct);

        await _repo.SaveChangesAsync(ct);

        _logger.LogInformation("SalesOrder {OrderId} cancelled by user {CallerId}", id, callerId);

        var updated = await _repo.GetByIdWithItemsAsync(id, ct);
        return MapToDto(updated!);
    }

    // ── Mapping helpers ────────────────────────────────────────────────────

    private static SalesOrderDto MapToDto(
        SalesOrder o,
        IEnumerable<SalesOrderHistory>? history = null,
        Dictionary<int, string?>? performers = null)
    {
        var items = o.Items?.Select(MapItemToDto) ?? [];
        var total = o.Items?.Sum(i => i.Quantity * i.UnitPrice * (1 - i.Discount / 100)) ?? 0m;

        return new SalesOrderDto(
            Id: o.Id,
            OrderNumber: o.OrderNumber,
            DistributorId: o.DistributorId,
            DistributorName: o.Distributor?.Name ?? string.Empty,
            Status: o.Status,
            StatusLabel: o.Status.ToString(),
            Notes: o.Notes,
            Items: items,
            History: (history ?? Enumerable.Empty<SalesOrderHistory>()).Select(h => new SalesOrderHistoryDto(
                h.Id,
                h.Action,
                h.FromStatus,
                h.ToStatus,
                h.PerformedBy,
                performers?.GetValueOrDefault(h.PerformedBy),
                h.PerformedAt,
                h.Notes
            )),
            TotalAmount: decimal.Round(total, 2),
            SubmittedBy: o.SubmittedBy,
            SubmittedAt: o.SubmittedAt,
            RepApprovedBy: o.RepApprovedBy,
            RepApprovedAt: o.RepApprovedAt,
            ManagerApprovedBy: o.ManagerApprovedBy,
            ManagerApprovedAt: o.ManagerApprovedAt,
            FinalizedBy: o.FinalizedBy,
            FinalizedAt: o.FinalizedAt,
            CancelledBy: o.CancelledBy,
            CancelledAt: o.CancelledAt,
            CancelReason: o.CancelReason,
            AcknowledgedBy: o.AcknowledgedBy,
            AcknowledgedAt: o.AcknowledgedAt,
            IsActive: o.IsActive,
            CreatedAt: o.CreatedAt,
            UpdatedAt: o.UpdatedAt,
            CreatedBy: o.CreatedBy,
            UpdatedBy: o.UpdatedBy
        );
    }

    private static SalesOrderSummaryDto MapToSummaryDto(SalesOrder o)
    {
        var total = o.Items?.Sum(i => i.Quantity * i.UnitPrice * (1 - i.Discount / 100)) ?? 0m;
        return new SalesOrderSummaryDto(
            Id: o.Id,
            OrderNumber: o.OrderNumber,
            DistributorId: o.DistributorId,
            DistributorName: o.Distributor?.Name ?? string.Empty,
            Status: o.Status,
            StatusLabel: o.Status.ToString(),
            TotalAmount: decimal.Round(total, 2),
            ItemCount: o.Items?.Count ?? 0,
            IsActive: o.IsActive,
            CreatedAt: o.CreatedAt,
            UpdatedAt: o.UpdatedAt,
            SubmittedAt: o.SubmittedAt
        );
    }

    private static SalesOrderItemDto MapItemToDto(SalesOrderItem i)
    {
        var lineTotal = i.Quantity * i.UnitPrice * (1 - i.Discount / 100);
        return new SalesOrderItemDto(
            Id: i.Id,
            ProductId: i.ProductId,
            ProductCode: i.Product?.Code ?? string.Empty,
            ProductDescription: i.Product?.ItemDescription ?? string.Empty,
            Quantity: i.Quantity,
            UnitPrice: i.UnitPrice,
            Discount: i.Discount,
            LineTotal: decimal.Round(lineTotal, 2)
        );
    }
}
