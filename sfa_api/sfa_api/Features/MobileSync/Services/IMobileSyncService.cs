using sfa_api.Features.MobileSync.DTOs;

namespace sfa_api.Features.MobileSync.Services;

public interface IMobileSyncService
{
    Task<MobileProductListDto> GetProductsAsync(CancellationToken ct = default);
}
