using sfa_api.Features.Reports.DTOs;
using sfa_api.Features.Reports.Requests;

namespace sfa_api.Features.Reports.Services;

public interface ISalesSummaryService
{
    Task<SalesSummaryResponseDto> GetSalesSummaryAsync(
        SalesSummaryQuery query, CancellationToken ct = default);
}
