using sfa_api.Features.SalesOrders.DTOs;
using sfa_api.Features.SalesOrders.Enums;
using sfa_api.Features.SalesOrders.Requests;
using sfa_api.Features.Users.Entities;

namespace sfa_api.Features.SalesOrders.Services;

public interface ISalesOrderService
{
    Task<SalesOrderDto> GetByIdAsync(int id, int callerId, UserRole callerRole, CancellationToken ct = default);
    Task<SalesOrderListDto> GetAllAsync(int page, int pageSize, string? search, SalesOrderStatus? status, DateTime? fromDate, DateTime? toDate, int callerId, UserRole callerRole, CancellationToken ct = default);
    Task<SalesOrderDto> CreateAsync(CreateSalesOrderRequest request, int callerId, UserRole callerRole, CancellationToken ct = default);
    Task<SalesOrderDto> UpdateAsync(int id, UpdateSalesOrderRequest request, int callerId, UserRole callerRole, CancellationToken ct = default);
    Task<SalesOrderDto> SubmitAsync(int id, int callerId, UserRole callerRole, CancellationToken ct = default);
    Task<SalesOrderDto> RepApproveAsync(int id, int callerId, UserRole callerRole, CancellationToken ct = default);
    Task<SalesOrderDto> ApproveAsync(int id, int callerId, UserRole callerRole, CancellationToken ct = default);
    Task<SalesOrderDto> RejectAsync(int id, RejectSalesOrderRequest request, int callerId, UserRole callerRole, CancellationToken ct = default);
    Task<SalesOrderDto> AcknowledgeAsync(int id, int callerId, UserRole callerRole, CancellationToken ct = default);
    Task<SalesOrderDto> FinalizeAsync(int id, int callerId, UserRole callerRole, CancellationToken ct = default);
    Task<SalesOrderDto> CancelAsync(int id, RejectSalesOrderRequest request, int callerId, UserRole callerRole, CancellationToken ct = default);
}
