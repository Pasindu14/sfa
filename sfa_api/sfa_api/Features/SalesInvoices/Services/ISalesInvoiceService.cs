using sfa_api.Common.Errors;
using sfa_api.Features.SalesInvoices.DTOs;
using sfa_api.Features.SalesInvoices.Requests;
using sfa_api.Features.Users.Entities;

namespace sfa_api.Features.SalesInvoices.Services;

public interface ISalesInvoiceService
{
    Task<ImportBatchResultDto> ImportAsync(ImportSalesInvoicesRequest request, int callerId, CancellationToken ct = default);
    Task<(List<SalesInvoiceListDto> Items, int TotalCount)> GetListAsync(
        int page, int pageSize, string? search, string? status,
        DateOnly? dateFrom, DateOnly? dateTo, int? distributorId,
        int callerId, UserRole callerRole, CancellationToken ct = default);
    Task<SalesInvoiceDetailDto> GetDetailAsync(int id, int callerId, UserRole callerRole, CancellationToken ct = default);
}
