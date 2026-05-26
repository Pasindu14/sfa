using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using sfa_api.Common.Errors;
using sfa_api.Features.Distributors.Repositories;
using sfa_api.Features.PurchaseOrders.DTOs;
using sfa_api.Features.PurchaseOrders.Entities;
using sfa_api.Features.PurchaseOrders.Enums;
using sfa_api.Features.PurchaseOrders.Repositories;
using sfa_api.Features.PurchaseOrders.Requests;
using sfa_api.Features.UserGeoAssignments.Repositories;
using sfa_api.Features.Users.Entities;
using sfa_api.Features.Users.Repositories;
using sfa_api.Infrastructure.Locking;
using sfa_api.Infrastructure.Notifications;
using sfa_api.Infrastructure.Persistence;

namespace sfa_api.Features.PurchaseOrders.Services;

public class PurchaseOrderService(
    IPurchaseOrderRepository repo,
    IUserRepository userRepo,
    IUserGeoAssignmentRepository geoRepo,
    IDistributorRepository distributorRepo,
    AppDbContext context,
    IDistributedLockService lockService,
    INotificationService notificationService,
    ILogger<PurchaseOrderService> logger) : IPurchaseOrderService
{
    private readonly IPurchaseOrderRepository _repo = repo;
    private readonly IUserRepository _userRepo = userRepo;
    private readonly IUserGeoAssignmentRepository _geoRepo = geoRepo;
    private readonly IDistributorRepository _distributorRepo = distributorRepo;
    private readonly AppDbContext _context = context;
    private readonly IDistributedLockService _lockService = lockService;
    private readonly INotificationService _notificationService = notificationService;
    private readonly ILogger<PurchaseOrderService> _logger = logger;

    public async Task<PurchaseOrderDto> GetByIdAsync(int id, int callerId, UserRole callerRole, CancellationToken ct = default)
    {
        var order = await _repo.GetByIdWithItemsAsync(id, ct)
            ?? throw new NotFoundException("PurchaseOrder", id);

        // Distributors may only view their own orders
        if (callerRole == UserRole.Distributor)
        {
            var caller = await _userRepo.GetUserByIdAsync(callerId, ct)
                ?? throw new NotFoundException("User", callerId);
            if (order.DistributorId != caller.DistributorId)
                throw new AuthorizationException("this purchase order");
        }

        // Load history and resolve performer names (single batch query)
        var history = await _repo.GetHistoryAsync(id, ct);
        var performerIds = history.Select(h => h.PerformedBy).Distinct();
        var performers = await _userRepo.GetNamesByIdsAsync(performerIds, ct);

        return MapToDto(order, history, performers);
    }

    public async Task<PurchaseOrderListDto> GetAllAsync(
        int page,
        int pageSize,
        string? search,
        PurchaseOrderStatus? status,
        DateOnly? fromDate,
        DateOnly? toDate,
        int callerId,
        UserRole callerRole,
        CancellationToken ct = default)
    {
        var skip = (page - 1) * pageSize;

        IEnumerable<int>? distributorIds = null;

        if (callerRole == UserRole.Distributor)
        {
            var caller = await _userRepo.GetUserByIdAsync(callerId, ct)
                ?? throw new NotFoundException("User", callerId);
            if (caller.DistributorId.HasValue)
                distributorIds = [caller.DistributorId.Value];
        }
        else if (callerRole == UserRole.SalesRep)
        {
            var geo = await _geoRepo.GetActiveByUserIdAsync(callerId, ct);
            if (geo?.TerritoryId.HasValue == true)
            {
                var distributor = await _distributorRepo.GetByTerritoryIdAsync(geo.TerritoryId.Value, ct);
                distributorIds = distributor != null ? [distributor.Id] : [];
            }
            else
            {
                distributorIds = [];
            }
        }

        var (orders, totalCount) = await _repo.GetAllAsync(
            skip, pageSize, search, status, distributorIds, fromDate, toDate, ct);

        return new PurchaseOrderListDto(
            PurchaseOrders: orders.Select(MapToSummaryDto),
            TotalCount: totalCount,
            Page: page,
            PageSize: pageSize
        );
    }

    public async Task<PurchaseOrderDto> CreateAsync(CreatePurchaseOrderRequest request, int callerId, UserRole callerRole, CancellationToken ct = default)
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
                throw new AuthorizationException("purchase orders (no distributor assigned)");
            distributorId = caller.DistributorId.Value;
        }

        PurchaseOrder? order = null;

        var strategy = _context.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _context.Database.BeginTransactionAsync(ct);
            try
            {
                var seq = await _repo.GetNextOrderNumberAsync(ct);
                var orderNumber = $"PO-{DateTime.UtcNow.Year}-{seq:D5}";

                order = new PurchaseOrder
                {
                    OrderNumber = orderNumber,
                    DistributorId = distributorId,
                    Status = PurchaseOrderStatus.Draft,
                    Notes = request.Notes,
                    IsActive = true,
                    CreatedBy = callerId,
                    UpdatedBy = callerId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _repo.CreateAsync(order, ct);
                await _repo.SaveChangesAsync(ct);

                var items = request.Items.Select(i => new PurchaseOrderItem
                {
                    PurchaseOrderId = order.Id,
                    ProductId = i.ProductId,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    Discount = i.Discount
                }).ToList();

                await _repo.AddItemsAsync(items, ct);

                var snapshot = JsonSerializer.Serialize(request.Items);
                await _repo.AddHistoryAsync(new PurchaseOrderHistory
                {
                    PurchaseOrderId = order.Id,
                    Action = "Created",
                    FromStatus = null,
                    ToStatus = PurchaseOrderStatus.Draft,
                    PerformedBy = callerId,
                    PerformedAt = DateTime.UtcNow,
                    Notes = request.Notes,
                    ItemsSnapshot = snapshot
                }, ct);

                await _repo.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);
            }
            catch
            {
                await tx.RollbackAsync(ct);
                throw;
            }
        });

        _logger.LogInformation("PurchaseOrder {OrderNumber} created by user {CallerId}", order!.OrderNumber, callerId);

        var created = await _repo.GetByIdWithItemsAsync(order!.Id, ct);
        return MapToDto(created!);
    }

    public async Task<PurchaseOrderDto> UpdateAsync(int id, UpdatePurchaseOrderRequest request, int callerId, UserRole callerRole, CancellationToken ct = default)
    {
        await using var @lock = await _lockService.AcquireAsync($"po:transition:{id}", ct)
            ?? throw new ConcurrencyConflictException(new { orderId = id, message = "Another operation is already in progress for this purchase order." });

        var order = await _repo.GetByIdWithItemsAsync(id, ct)
            ?? throw new NotFoundException("PurchaseOrder", id);

        var allowedStatuses = callerRole switch
        {
            UserRole.Distributor => new[] { PurchaseOrderStatus.Draft },
            UserRole.SalesRep    => new[] { PurchaseOrderStatus.PendingRepApproval },
            UserRole.Supervisor     => new[] { PurchaseOrderStatus.PendingManagerApproval },
            UserRole.Admin       => new[] { PurchaseOrderStatus.Draft, PurchaseOrderStatus.PendingRepApproval, PurchaseOrderStatus.PendingManagerApproval },
            _                    => Array.Empty<PurchaseOrderStatus>()
        };

        if (!allowedStatuses.Contains(order.Status))
            throw new BusinessRuleException("ORDER_NOT_EDITABLE", "Order cannot be edited at this stage.");

        // Distributors may only update their own orders
        if (callerRole == UserRole.Distributor)
        {
            var caller = await _userRepo.GetUserByIdAsync(callerId, ct)
                ?? throw new NotFoundException("User", callerId);
            if (order.DistributorId != caller.DistributorId)
                throw new AuthorizationException("this purchase order");
        }

        var updateStrategy = _context.Database.CreateExecutionStrategy();
        await updateStrategy.ExecuteAsync(async () =>
        {
            await using var tx = await _context.Database.BeginTransactionAsync(ct);
            try
            {
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

                await _repo.RemoveItemsAsync(id, ct);
                var newItems = request.Items.Select(i => new PurchaseOrderItem
                {
                    PurchaseOrderId = id,
                    ProductId = i.ProductId,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    Discount = i.Discount
                }).ToList();
                await _repo.AddItemsAsync(newItems, ct);

                var afterSnapshot = JsonSerializer.Serialize(request.Items);
                await _repo.AddHistoryAsync(new PurchaseOrderHistory
                {
                    PurchaseOrderId = id,
                    Action = "ItemsEdited",
                    FromStatus = order.Status,
                    ToStatus = order.Status,
                    PerformedBy = callerId,
                    PerformedAt = DateTime.UtcNow,
                    Notes = beforeSnapshot,
                    ItemsSnapshot = afterSnapshot
                }, ct);

                await _repo.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);
            }
            catch { await tx.RollbackAsync(ct); throw; }
        });

        _logger.LogInformation("PurchaseOrder {OrderId} updated by user {CallerId}", id, callerId);

        var updated = await _repo.GetByIdWithItemsAsync(id, ct);
        return MapToDto(updated!);
    }

    public async Task<PurchaseOrderDto> SubmitAsync(int id, int callerId, UserRole callerRole, CancellationToken ct = default)
    {
        await using var @lock = await _lockService.AcquireAsync($"po:transition:{id}", ct)
            ?? throw new ConcurrencyConflictException(new { orderId = id, message = "Another operation is already in progress for this purchase order." });

        if (callerRole != UserRole.Distributor && callerRole != UserRole.Admin)
            throw new AuthorizationException("submit purchase orders");

        var order = await _repo.GetByIdWithItemsAsync(id, ct)
            ?? throw new NotFoundException("PurchaseOrder", id);

        if (order.Status != PurchaseOrderStatus.Draft)
            throw new BusinessRuleException("ORDER_NOT_SUBMITTABLE", "Only Draft orders can be submitted.");

        if (callerRole == UserRole.Distributor)
        {
            var caller = await _userRepo.GetUserByIdAsync(callerId, ct)
                ?? throw new NotFoundException("User", callerId);
            if (order.DistributorId != caller.DistributorId)
                throw new AuthorizationException("this purchase order");
        }

        var submitStrategy = _context.Database.CreateExecutionStrategy();
        await submitStrategy.ExecuteAsync(async () =>
        {
            await using var tx = await _context.Database.BeginTransactionAsync(ct);
            try
            {
                var fromStatus = order.Status;
                order.Status = PurchaseOrderStatus.PendingRepApproval;
                order.SubmittedBy = callerId;
                order.SubmittedAt = DateTime.UtcNow;
                order.UpdatedBy = callerId;
                order.UpdatedAt = DateTime.UtcNow;

                await _repo.UpdateAsync(order, ct);
                await _repo.AddHistoryAsync(new PurchaseOrderHistory
                {
                    PurchaseOrderId = id,
                    Action = "Submitted",
                    FromStatus = fromStatus,
                    ToStatus = PurchaseOrderStatus.PendingRepApproval,
                    PerformedBy = callerId,
                    PerformedAt = DateTime.UtcNow
                }, ct);

                await _repo.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);
            }
            catch { await tx.RollbackAsync(ct); throw; }
        });

        _logger.LogInformation("PurchaseOrder {OrderId} submitted by user {CallerId}", id, callerId);

        await _notificationService.SendToDistributorSalesRepsAsync(
            order.DistributorId,
            "New Purchase Order",
            $"Order {order.OrderNumber} has been submitted and is awaiting your approval.",
            new Dictionary<string, string> { ["orderId"] = id.ToString(), ["orderNumber"] = order.OrderNumber },
            ct);

        var updated = await _repo.GetByIdWithItemsAsync(id, ct);
        return MapToDto(updated!);
    }

    public async Task<PurchaseOrderDto> RepApproveAsync(int id, int callerId, UserRole callerRole, CancellationToken ct = default)
    {
        await using var @lock = await _lockService.AcquireAsync($"po:transition:{id}", ct)
            ?? throw new ConcurrencyConflictException(new { orderId = id, message = "Another operation is already in progress for this purchase order." });

        if (callerRole != UserRole.SalesRep && callerRole != UserRole.Admin)
            throw new AuthorizationException("rep-approve purchase orders");

        var order = await _repo.GetByIdWithItemsAsync(id, ct)
            ?? throw new NotFoundException("PurchaseOrder", id);

        if (order.Status != PurchaseOrderStatus.PendingRepApproval)
            throw new BusinessRuleException("ORDER_NOT_PENDING_REP_APPROVAL", "Order is not in PendingRepApproval status.");

        var repApproveStrategy = _context.Database.CreateExecutionStrategy();
        await repApproveStrategy.ExecuteAsync(async () =>
        {
            await using var tx = await _context.Database.BeginTransactionAsync(ct);
            try
            {
                var fromStatus = order.Status;
                order.Status = PurchaseOrderStatus.PendingManagerApproval;
                order.RepApprovedBy = callerId;
                order.RepApprovedAt = DateTime.UtcNow;
                order.UpdatedBy = callerId;
                order.UpdatedAt = DateTime.UtcNow;

                await _repo.UpdateAsync(order, ct);
                await _repo.AddHistoryAsync(new PurchaseOrderHistory
                {
                    PurchaseOrderId = id,
                    Action = "RepApproved",
                    FromStatus = fromStatus,
                    ToStatus = PurchaseOrderStatus.PendingManagerApproval,
                    PerformedBy = callerId,
                    PerformedAt = DateTime.UtcNow
                }, ct);

                await _repo.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);
            }
            catch { await tx.RollbackAsync(ct); throw; }
        });

        _logger.LogInformation("PurchaseOrder {OrderId} rep-approved by user {CallerId}", id, callerId);

        var updated = await _repo.GetByIdWithItemsAsync(id, ct);
        return MapToDto(updated!);
    }

    public async Task<PurchaseOrderDto> ApproveAsync(int id, int callerId, UserRole callerRole, CancellationToken ct = default)
    {
        await using var @lock = await _lockService.AcquireAsync($"po:transition:{id}", ct)
            ?? throw new ConcurrencyConflictException(new { orderId = id, message = "Another operation is already in progress for this purchase order." });

        if (callerRole != UserRole.Supervisor && callerRole != UserRole.Admin)
            throw new AuthorizationException("approve purchase orders");

        var order = await _repo.GetByIdWithItemsAsync(id, ct)
            ?? throw new NotFoundException("PurchaseOrder", id);

        if (order.Status != PurchaseOrderStatus.PendingManagerApproval)
            throw new BusinessRuleException("ORDER_NOT_PENDING_MANAGER_APPROVAL", "Order is not in PendingManagerApproval status.");

        var approveStrategy = _context.Database.CreateExecutionStrategy();
        await approveStrategy.ExecuteAsync(async () =>
        {
            await using var tx = await _context.Database.BeginTransactionAsync(ct);
            try
            {
                var fromStatus = order.Status;
                order.Status = PurchaseOrderStatus.PendingDistributorFinalization;
                order.ManagerApprovedBy = callerId;
                order.ManagerApprovedAt = DateTime.UtcNow;
                order.UpdatedBy = callerId;
                order.UpdatedAt = DateTime.UtcNow;

                await _repo.UpdateAsync(order, ct);
                await _repo.AddHistoryAsync(new PurchaseOrderHistory
                {
                    PurchaseOrderId = id,
                    Action = "ManagerApproved",
                    FromStatus = fromStatus,
                    ToStatus = PurchaseOrderStatus.PendingDistributorFinalization,
                    PerformedBy = callerId,
                    PerformedAt = DateTime.UtcNow
                }, ct);

                await _repo.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);
            }
            catch { await tx.RollbackAsync(ct); throw; }
        });

        _logger.LogInformation("PurchaseOrder {OrderId} manager-approved by user {CallerId}", id, callerId);

        var updated = await _repo.GetByIdWithItemsAsync(id, ct);
        return MapToDto(updated!);
    }

    public async Task<PurchaseOrderDto> RejectAsync(int id, RejectPurchaseOrderRequest request, int callerId, UserRole callerRole, CancellationToken ct = default)
    {
        await using var @lock = await _lockService.AcquireAsync($"po:transition:{id}", ct)
            ?? throw new ConcurrencyConflictException(new { orderId = id, message = "Another operation is already in progress for this purchase order." });

        var order = await _repo.GetByIdWithItemsAsync(id, ct)
            ?? throw new NotFoundException("PurchaseOrder", id);

        // Role + status gate
        if (order.Status == PurchaseOrderStatus.PendingRepApproval
            && callerRole != UserRole.SalesRep && callerRole != UserRole.Admin)
            throw new AuthorizationException("reject orders at this stage (SalesRep or Admin only)");

        if (order.Status == PurchaseOrderStatus.PendingManagerApproval
            && callerRole != UserRole.Supervisor && callerRole != UserRole.Admin)
            throw new AuthorizationException("reject orders at this stage (Manager or Admin only)");

        if (order.Status != PurchaseOrderStatus.PendingRepApproval
            && order.Status != PurchaseOrderStatus.PendingManagerApproval)
            throw new BusinessRuleException("ORDER_NOT_REJECTABLE", "Order cannot be rejected at this stage.");

        // Capture before the transaction lambda mutates order.Status
        var rejectedFromManagerStage = order.Status == PurchaseOrderStatus.PendingManagerApproval;

        var rejectStrategy = _context.Database.CreateExecutionStrategy();
        await rejectStrategy.ExecuteAsync(async () =>
        {
            await using var tx = await _context.Database.BeginTransactionAsync(ct);
            try
            {
                var fromStatus = order.Status;
                order.Status = PurchaseOrderStatus.PendingDistributorAcknowledgement;
                order.CancelReason = request.Reason;
                order.UpdatedBy = callerId;
                order.UpdatedAt = DateTime.UtcNow;

                await _repo.UpdateAsync(order, ct);
                await _repo.AddHistoryAsync(new PurchaseOrderHistory
                {
                    PurchaseOrderId = id,
                    Action = "Rejected",
                    FromStatus = fromStatus,
                    ToStatus = PurchaseOrderStatus.PendingDistributorAcknowledgement,
                    PerformedBy = callerId,
                    PerformedAt = DateTime.UtcNow,
                    Notes = request.Reason
                }, ct);

                await _repo.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);
            }
            catch { await tx.RollbackAsync(ct); throw; }
        });

        _logger.LogInformation("PurchaseOrder {OrderId} rejected by user {CallerId}", id, callerId);

        var notificationData = new Dictionary<string, string>
        {
            ["orderId"] = id.ToString(),
            ["orderNumber"] = order.OrderNumber
        };

        // Always notify the distributor — they must acknowledge the rejection
        await _notificationService.SendToDistributorUsersAsync(
            order.DistributorId,
            "Purchase Order Rejected",
            $"Order {order.OrderNumber} has been rejected. Please review the reason and acknowledge.",
            notificationData,
            ct);

        // When a supervisor/manager rejects, also notify the sales rep who forwarded it
        if (rejectedFromManagerStage)
        {
            await _notificationService.SendToDistributorSalesRepsAsync(
                order.DistributorId,
                "Purchase Order Rejected by Manager",
                $"Order {order.OrderNumber} you approved was rejected by the manager.",
                notificationData,
                ct);
        }

        var updated = await _repo.GetByIdWithItemsAsync(id, ct);
        return MapToDto(updated!);
    }

    public async Task<PurchaseOrderDto> AcknowledgeAsync(int id, int callerId, UserRole callerRole, CancellationToken ct = default)
    {
        await using var @lock = await _lockService.AcquireAsync($"po:transition:{id}", ct)
            ?? throw new ConcurrencyConflictException(new { orderId = id, message = "Another operation is already in progress for this purchase order." });

        if (callerRole != UserRole.Distributor && callerRole != UserRole.Admin)
            throw new AuthorizationException("acknowledge purchase order rejections");

        var order = await _repo.GetByIdWithItemsAsync(id, ct)
            ?? throw new NotFoundException("PurchaseOrder", id);

        if (order.Status != PurchaseOrderStatus.PendingDistributorAcknowledgement)
            throw new BusinessRuleException("ORDER_NOT_PENDING_ACKNOWLEDGEMENT",
                "Order is not pending Distributor acknowledgement.");

        if (callerRole == UserRole.Distributor)
        {
            var caller = await _userRepo.GetUserByIdAsync(callerId, ct)
                ?? throw new NotFoundException("User", callerId);
            if (order.DistributorId != caller.DistributorId)
                throw new AuthorizationException("this purchase order");
        }

        var ackStrategy = _context.Database.CreateExecutionStrategy();
        await ackStrategy.ExecuteAsync(async () =>
        {
            await using var tx = await _context.Database.BeginTransactionAsync(ct);
            try
            {
                var fromStatus = order.Status;
                order.Status = PurchaseOrderStatus.Cancelled;
                order.AcknowledgedBy = callerId;
                order.AcknowledgedAt = DateTime.UtcNow;
                order.CancelledBy = callerId;
                order.CancelledAt = DateTime.UtcNow;
                order.UpdatedBy = callerId;
                order.UpdatedAt = DateTime.UtcNow;

                await _repo.UpdateAsync(order, ct);
                await _repo.AddHistoryAsync(new PurchaseOrderHistory
                {
                    PurchaseOrderId = id,
                    Action = "RejectionAcknowledged",
                    FromStatus = fromStatus,
                    ToStatus = PurchaseOrderStatus.Cancelled,
                    PerformedBy = callerId,
                    PerformedAt = DateTime.UtcNow
                }, ct);

                await _repo.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);
            }
            catch { await tx.RollbackAsync(ct); throw; }
        });

        _logger.LogInformation("PurchaseOrder {OrderId} rejection acknowledged by user {CallerId}", id, callerId);

        var updated = await _repo.GetByIdWithItemsAsync(id, ct);
        return MapToDto(updated!);
    }

    public async Task<PurchaseOrderDto> FinalizeAsync(int id, int callerId, UserRole callerRole, CancellationToken ct = default)
    {
        await using var @lock = await _lockService.AcquireAsync($"po:transition:{id}", ct)
            ?? throw new ConcurrencyConflictException(new { orderId = id, message = "Another operation is already in progress for this purchase order." });

        if (callerRole != UserRole.Distributor && callerRole != UserRole.Admin)
            throw new AuthorizationException("finalize purchase orders");

        var order = await _repo.GetByIdWithItemsAsync(id, ct)
            ?? throw new NotFoundException("PurchaseOrder", id);

        if (order.Status != PurchaseOrderStatus.PendingDistributorFinalization)
            throw new BusinessRuleException("ORDER_NOT_PENDING_FINALIZATION", "Order is not in PendingDistributorFinalization status.");

        if (callerRole == UserRole.Distributor)
        {
            var caller = await _userRepo.GetUserByIdAsync(callerId, ct)
                ?? throw new NotFoundException("User", callerId);
            if (order.DistributorId != caller.DistributorId)
                throw new AuthorizationException("this purchase order");
        }

        var finalizeStrategy = _context.Database.CreateExecutionStrategy();
        await finalizeStrategy.ExecuteAsync(async () =>
        {
            await using var tx = await _context.Database.BeginTransactionAsync(ct);
            try
            {
                var fromStatus = order.Status;
                order.Status = PurchaseOrderStatus.Finalized;
                order.FinalizedBy = callerId;
                order.FinalizedAt = DateTime.UtcNow;
                order.UpdatedBy = callerId;
                order.UpdatedAt = DateTime.UtcNow;

                await _repo.UpdateAsync(order, ct);
                await _repo.AddHistoryAsync(new PurchaseOrderHistory
                {
                    PurchaseOrderId = id,
                    Action = "Finalized",
                    FromStatus = fromStatus,
                    ToStatus = PurchaseOrderStatus.Finalized,
                    PerformedBy = callerId,
                    PerformedAt = DateTime.UtcNow
                }, ct);

                await _repo.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);
            }
            catch { await tx.RollbackAsync(ct); throw; }
        });

        _logger.LogInformation("PurchaseOrder {OrderId} finalized by user {CallerId}", id, callerId);

        var updated = await _repo.GetByIdWithItemsAsync(id, ct);
        return MapToDto(updated!);
    }

    public async Task<PurchaseOrderDto> CancelAsync(int id, RejectPurchaseOrderRequest request, int callerId, UserRole callerRole, CancellationToken ct = default)
    {
        await using var @lock = await _lockService.AcquireAsync($"po:transition:{id}", ct)
            ?? throw new ConcurrencyConflictException(new { orderId = id, message = "Another operation is already in progress for this purchase order." });

        var order = await _repo.GetByIdWithItemsAsync(id, ct)
            ?? throw new NotFoundException("PurchaseOrder", id);

        // Distributors may only cancel Draft orders
        if (callerRole == UserRole.Distributor && order.Status != PurchaseOrderStatus.Draft)
            throw new BusinessRuleException("ORDER_NOT_CANCELLABLE", "Distributors can only cancel Draft orders.");

        if (callerRole != UserRole.Distributor && callerRole != UserRole.Admin)
            throw new AuthorizationException("cancel purchase orders");

        if (order.Status == PurchaseOrderStatus.Finalized || order.Status == PurchaseOrderStatus.Cancelled)
            throw new BusinessRuleException("ORDER_NOT_CANCELLABLE", "Order is already finalized or cancelled.");

        if (callerRole == UserRole.Distributor)
        {
            var caller = await _userRepo.GetUserByIdAsync(callerId, ct)
                ?? throw new NotFoundException("User", callerId);
            if (order.DistributorId != caller.DistributorId)
                throw new AuthorizationException("this purchase order");
        }

        var cancelStrategy = _context.Database.CreateExecutionStrategy();
        await cancelStrategy.ExecuteAsync(async () =>
        {
            await using var tx = await _context.Database.BeginTransactionAsync(ct);
            try
            {
                var fromStatus = order.Status;
                order.Status = PurchaseOrderStatus.Cancelled;
                order.CancelledBy = callerId;
                order.CancelledAt = DateTime.UtcNow;
                order.CancelReason = request.Reason;
                order.UpdatedBy = callerId;
                order.UpdatedAt = DateTime.UtcNow;

                await _repo.UpdateAsync(order, ct);
                await _repo.AddHistoryAsync(new PurchaseOrderHistory
                {
                    PurchaseOrderId = id,
                    Action = "Cancelled",
                    FromStatus = fromStatus,
                    ToStatus = PurchaseOrderStatus.Cancelled,
                    PerformedBy = callerId,
                    PerformedAt = DateTime.UtcNow,
                    Notes = request.Reason
                }, ct);

                await _repo.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);
            }
            catch { await tx.RollbackAsync(ct); throw; }
        });

        _logger.LogInformation("PurchaseOrder {OrderId} cancelled by user {CallerId}", id, callerId);

        var updated = await _repo.GetByIdWithItemsAsync(id, ct);
        return MapToDto(updated!);
    }

    public async Task<PurchaseOrderStatsDto> GetStatsAsync(
        int callerId, UserRole callerRole,
        DateOnly? fromDate, DateOnly? toDate,
        CancellationToken ct = default)
    {
        int? distributorFilter = null;
        if (callerRole == UserRole.Distributor)
        {
            var caller = await _userRepo.GetUserByIdAsync(callerId, ct)
                ?? throw new NotFoundException("User", callerId);
            distributorFilter = caller.DistributorId;
        }

        var counts = await _repo.GetCountsByStatusAsync(distributorFilter, fromDate, toDate, ct);

        counts.TryGetValue(PurchaseOrderStatus.PendingRepApproval, out var rep);
        counts.TryGetValue(PurchaseOrderStatus.PendingManagerApproval, out var mgr);
        counts.TryGetValue(PurchaseOrderStatus.PendingDistributorAcknowledgement, out var ack);
        counts.TryGetValue(PurchaseOrderStatus.Finalized, out var fin);

        return new PurchaseOrderStatsDto(rep, mgr, ack, fin, counts.Values.Sum());
    }

    // ── Mapping helpers ────────────────────────────────────────────────────

    private static PurchaseOrderDto MapToDto(
        PurchaseOrder o,
        IEnumerable<PurchaseOrderHistory>? history = null,
        Dictionary<int, string?>? performers = null)
    {
        var items = o.Items?.Select(MapItemToDto) ?? [];
        var total = o.Items?.Sum(i => i.Quantity * i.UnitPrice * (1 - i.Discount / 100)) ?? 0m;

        return new PurchaseOrderDto(
            Id: o.Id,
            OrderNumber: o.OrderNumber,
            DistributorId: o.DistributorId,
            DistributorName: o.Distributor?.Name ?? string.Empty,
            DistributorCategory: o.Distributor?.Category ?? "A",
            Status: o.Status,
            StatusLabel: o.Status.ToString(),
            Notes: o.Notes,
            Items: items,
            History: (history ?? Enumerable.Empty<PurchaseOrderHistory>()).Select(h => new PurchaseOrderHistoryDto(
                h.Id,
                h.Action,
                h.FromStatus,
                h.ToStatus,
                h.PerformedBy,
                performers?.GetValueOrDefault(h.PerformedBy),
                h.PerformedAt,
                h.Notes,
                h.ItemsSnapshot
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

    private static PurchaseOrderSummaryDto MapToSummaryDto(PurchaseOrder o)
    {
        var total = o.Items?.Sum(i => i.Quantity * i.UnitPrice * (1 - i.Discount / 100)) ?? 0m;
        return new PurchaseOrderSummaryDto(
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

    private static PurchaseOrderItemDto MapItemToDto(PurchaseOrderItem i)
    {
        var lineTotal = i.Quantity * i.UnitPrice * (1 - i.Discount / 100);
        return new PurchaseOrderItemDto(
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
