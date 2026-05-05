using sfa_api.Features.SalesTargets.DTOs;
using sfa_api.Features.SalesTargets.Requests;

namespace sfa_api.Features.SalesTargets.Services;

public interface ISalesTargetImportService
{
    Task<ImportSalesTargetsResultDto> ImportAsync(
        ImportSalesTargetsRequest request,
        int callerId,
        CancellationToken ct = default);
}
