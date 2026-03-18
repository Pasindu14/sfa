using sfa_api.Features.PurchaseOrders.DTOs;
using sfa_api.Features.PurchaseOrders.Enums;
using sfa_api.Features.PurchaseOrders.Requests;
using sfa_api.Features.Users.Entities;

namespace sfa_api.Features.PurchaseOrders.Services;

public interface IPurchaseOrderService
{
    Task<PurchaseOrderDto> GetByIdAsync(int id, int callerId, UserRole callerRole, CancellationToken ct = default);
    Task<PurchaseOrderListDto> GetAllAsync(int page, int pageSize, string? search, PurchaseOrderStatus? status, DateTime? fromDate, DateTime? toDate, int callerId, UserRole callerRole, CancellationToken ct = default);
    Task<PurchaseOrderDto> CreateAsync(CreatePurchaseOrderRequest request, int callerId, UserRole callerRole, CancellationToken ct = default);
    Task<PurchaseOrderDto> UpdateAsync(int id, UpdatePurchaseOrderRequest request, int callerId, UserRole callerRole, CancellationToken ct = default);
    Task<PurchaseOrderDto> SubmitAsync(int id, int callerId, UserRole callerRole, CancellationToken ct = default);
    Task<PurchaseOrderDto> RepApproveAsync(int id, int callerId, UserRole callerRole, CancellationToken ct = default);
    Task<PurchaseOrderDto> ApproveAsync(int id, int callerId, UserRole callerRole, CancellationToken ct = default);
    Task<PurchaseOrderDto> RejectAsync(int id, RejectPurchaseOrderRequest request, int callerId, UserRole callerRole, CancellationToken ct = default);
    Task<PurchaseOrderDto> AcknowledgeAsync(int id, int callerId, UserRole callerRole, CancellationToken ct = default);
    Task<PurchaseOrderDto> FinalizeAsync(int id, int callerId, UserRole callerRole, CancellationToken ct = default);
    Task<PurchaseOrderDto> CancelAsync(int id, RejectPurchaseOrderRequest request, int callerId, UserRole callerRole, CancellationToken ct = default);
    Task<PurchaseOrderStatsDto> GetStatsAsync(int callerId, UserRole callerRole, DateTime? fromDate, DateTime? toDate, CancellationToken ct = default);
}
