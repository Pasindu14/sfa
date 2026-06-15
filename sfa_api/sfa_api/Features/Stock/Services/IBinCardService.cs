using sfa_api.Features.Stock.DTOs;
using sfa_api.Features.Stock.Requests;

namespace sfa_api.Features.Stock.Services;

public interface IBinCardService
{
    Task<BinCardResponseDto> GetBinCardAsync(BinCardQuery query, CancellationToken ct = default);
}
