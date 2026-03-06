using Microsoft.EntityFrameworkCore;
using sfa_api.Features.Distributors.Entities;
using sfa_api.Infrastructure.Persistence;

namespace sfa_api.Features.Distributors.Repositories;

public class DistributorRepository(AppDbContext context) : IDistributorRepository
{
    private readonly AppDbContext _context = context;

    public async Task<Distributor?> GetByIdAsync(int id, CancellationToken ct = default)
        => await _context.Distributors.FindAsync([id], ct);

    public async Task<Distributor?> GetByEmailAsync(string email, CancellationToken ct = default)
        => await _context.Distributors.FirstOrDefaultAsync(d => d.Email == email, ct);

    public async Task<Distributor?> GetByPhoneAsync(string phone, CancellationToken ct = default)
        => await _context.Distributors.FirstOrDefaultAsync(d => d.Phone == phone, ct);

    public async Task<(IEnumerable<Distributor> Distributors, int TotalCount)> GetAllAsync(int skip, int take, CancellationToken ct = default)
    {
        var totalCount = await _context.Distributors.CountAsync(ct);
        var distributors = await _context.Distributors
            .OrderBy(d => d.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);
        return (distributors, totalCount);
    }

    public async Task CreateAsync(Distributor distributor, CancellationToken ct = default)
        => await _context.Distributors.AddAsync(distributor, ct);

    public Task UpdateAsync(Distributor distributor, CancellationToken ct = default)
    {
        _context.Distributors.Update(distributor);
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var distributor = await _context.Distributors.FindAsync([id], ct);
        if (distributor != null)
        {
            distributor.IsDeleted = true;
            _context.Distributors.Update(distributor);
        }
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await _context.SaveChangesAsync(ct);
}
