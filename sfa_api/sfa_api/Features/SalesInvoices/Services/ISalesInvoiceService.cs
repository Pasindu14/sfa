using sfa_api.Features.SalesInvoices.DTOs;
using sfa_api.Features.SalesInvoices.Requests;

namespace sfa_api.Features.SalesInvoices.Services;

public interface ISalesInvoiceService
{
    Task<ImportBatchResultDto> ImportAsync(ImportSalesInvoicesRequest request, int callerId, CancellationToken ct = default);
}
