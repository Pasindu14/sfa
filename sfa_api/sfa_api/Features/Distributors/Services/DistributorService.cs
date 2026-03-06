using sfa_api.Common.Errors;
using sfa_api.Features.Distributors.DTOs;
using sfa_api.Features.Distributors.Entities;
using sfa_api.Features.Distributors.Repositories;
using sfa_api.Features.Distributors.Requests;

namespace sfa_api.Features.Distributors.Services;

public class DistributorService(
    IDistributorRepository repo,
    ILogger<DistributorService> logger) : IDistributorService
{
    private readonly IDistributorRepository _repo = repo;
    private readonly ILogger<DistributorService> _logger = logger;

    public async Task<DistributorDto> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var distributor = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("Distributor", id);
        return MapToDto(distributor);
    }

    public async Task<DistributorListDto> GetAllAsync(int page, int pageSize, CancellationToken ct = default)
    {
        var skip = (page - 1) * pageSize;
        var (distributors, totalCount) = await _repo.GetAllAsync(skip, pageSize, ct);
        return new DistributorListDto(
            Distributors: distributors.Select(MapToDto),
            TotalCount: totalCount,
            Page: page,
            PageSize: pageSize
        );
    }

    public async Task<DistributorDto> CreateAsync(CreateDistributorRequest request, int? callerId, CancellationToken ct = default)
    {
        var existing = await _repo.GetByEmailAsync(request.Email, ct);
        if (existing != null)
            throw new DuplicateResourceException("Email");

        existing = await _repo.GetByPhoneAsync(request.Phone, ct);
        if (existing != null)
            throw new DuplicateResourceException("Phone");

        var distributor = new Distributor
        {
            Name = request.Name,
            Address = request.Address,
            Phone = request.Phone,
            Email = request.Email,
            Alias = request.Alias,
            TradeDiscount = request.TradeDiscount,
            Commission = request.Commission,
            Remark = request.Remark,
            VatRegNo = request.VatRegNo,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            IsActive = true,
            CreatedBy = callerId,
            UpdatedBy = callerId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _repo.CreateAsync(distributor, ct);
        await _repo.SaveChangesAsync(ct);

        _logger.LogInformation("Distributor {DistributorId} created", distributor.Id);
        return MapToDto(distributor);
    }

    public async Task<DistributorDto> UpdateAsync(int id, UpdateDistributorRequest request, int? callerId, CancellationToken ct = default)
    {
        var distributor = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("Distributor", id);

        var existing = await _repo.GetByEmailAsync(request.Email, ct);
        if (existing != null && existing.Id != id)
            throw new DuplicateResourceException("Email");

        existing = await _repo.GetByPhoneAsync(request.Phone, ct);
        if (existing != null && existing.Id != id)
            throw new DuplicateResourceException("Phone");

        distributor.Name = request.Name;
        distributor.Address = request.Address;
        distributor.Phone = request.Phone;
        distributor.Email = request.Email;
        distributor.Alias = request.Alias;
        distributor.TradeDiscount = request.TradeDiscount;
        distributor.Commission = request.Commission;
        distributor.Remark = request.Remark;
        distributor.VatRegNo = request.VatRegNo;
        distributor.Latitude = request.Latitude;
        distributor.Longitude = request.Longitude;
        distributor.UpdatedBy = callerId;
        distributor.UpdatedAt = DateTime.UtcNow;

        await _repo.UpdateAsync(distributor, ct);
        await _repo.SaveChangesAsync(ct);

        _logger.LogInformation("Distributor {DistributorId} updated", id);
        return MapToDto(distributor);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        _ = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("Distributor", id);

        await _repo.DeleteAsync(id, ct);
        await _repo.SaveChangesAsync(ct);

        _logger.LogInformation("Distributor {DistributorId} deleted", id);
    }

    public async Task ActivateAsync(int id, int? callerId, CancellationToken ct = default)
    {
        var distributor = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("Distributor", id);

        distributor.IsActive = true;
        distributor.UpdatedBy = callerId;
        distributor.UpdatedAt = DateTime.UtcNow;

        await _repo.UpdateAsync(distributor, ct);
        await _repo.SaveChangesAsync(ct);

        _logger.LogInformation("Distributor {DistributorId} activated", id);
    }

    public async Task DeactivateAsync(int id, int? callerId, CancellationToken ct = default)
    {
        var distributor = await _repo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("Distributor", id);

        distributor.IsActive = false;
        distributor.UpdatedBy = callerId;
        distributor.UpdatedAt = DateTime.UtcNow;

        await _repo.UpdateAsync(distributor, ct);
        await _repo.SaveChangesAsync(ct);

        _logger.LogInformation("Distributor {DistributorId} deactivated", id);
    }

    private static DistributorDto MapToDto(Distributor d) => new(
        Id: d.Id,
        Name: d.Name,
        Address: d.Address,
        Phone: d.Phone,
        Email: d.Email,
        Alias: d.Alias,
        TradeDiscount: d.TradeDiscount,
        Commission: d.Commission,
        Remark: d.Remark,
        VatRegNo: d.VatRegNo,
        Latitude: d.Latitude,
        Longitude: d.Longitude,
        IsActive: d.IsActive,
        CreatedAt: d.CreatedAt,
        UpdatedAt: d.UpdatedAt
    );
}
