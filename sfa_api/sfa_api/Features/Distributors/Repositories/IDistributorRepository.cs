using sfa_api.Features.Distributors.Entities;

namespace sfa_api.Features.Distributors.Repositories;

public interface IDistributorRepository
{
    Task<Distributor?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Distributor?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<Distributor?> GetByPhoneAsync(string phone, CancellationToken ct = default);
    Task<(IEnumerable<Distributor> Distributors, int TotalCount)> GetAllAsync(int skip, int take, CancellationToken ct = default);
    Task CreateAsync(Distributor distributor, CancellationToken ct = default);
    Task UpdateAsync(Distributor distributor, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
