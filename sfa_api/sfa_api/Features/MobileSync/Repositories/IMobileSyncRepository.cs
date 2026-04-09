using sfa_api.Features.MobileSync.DTOs;

namespace sfa_api.Features.MobileSync.Repositories;

public interface IMobileSyncRepository
{
    Task<List<MobileSyncProductDto>> GetActiveProductsAsync(CancellationToken ct = default);
}
