using Microsoft.EntityFrameworkCore;
using sfa_api.Common.Errors;
using sfa_api.Features.ProductCategoryPricings.DTOs;
using sfa_api.Features.ProductCategoryPricings.Entities;
using sfa_api.Features.ProductCategoryPricings.Requests;
using sfa_api.Infrastructure.Persistence;

namespace sfa_api.Features.ProductCategoryPricings.Repositories;

public class ProductCategoryPricingRepository(AppDbContext context) : IProductCategoryPricingRepository
{
    private readonly AppDbContext _context = context;

    private static readonly string[] Categories = ["A", "B", "C", "D"];

    public async Task<IEnumerable<ProductCategoryPricingDto>> GetAllWithPricingAsync(CancellationToken ct = default)
    {
        // Load all active non-deleted products (Product has no HasQueryFilter — IsActive and IsDeleted are filtered explicitly in Product repos)
        var products = await _context.Products
            .AsNoTracking()
            .Where(p => p.IsActive && !p.IsDeleted)
            .OrderBy(p => p.Code)
            .ToListAsync(ct);

        var productIds = products.Select(p => p.Id).ToList();

        // Load all pricing rows for those products
        var pricingRows = await _context.ProductCategoryPrices
            .AsNoTracking()
            .Where(x => productIds.Contains(x.ProductId))
            .ToListAsync(ct);

        // Group pricing by productId for O(1) lookup
        var pricingByProduct = pricingRows
            .GroupBy(x => x.ProductId)
            .ToDictionary(g => g.Key, g => g.ToDictionary(x => x.Category, x => x.Price));

        return products.Select(p =>
        {
            var prices = pricingByProduct.GetValueOrDefault(p.Id, []);
            return new ProductCategoryPricingDto(
                ProductId: p.Id,
                ProductCode: p.Code,
                ItemDescription: p.ItemDescription,
                PriceA: prices.GetValueOrDefault("A", 0m),
                PriceB: prices.GetValueOrDefault("B", 0m),
                PriceC: prices.GetValueOrDefault("C", 0m),
                PriceD: prices.GetValueOrDefault("D", 0m)
            );
        });
    }

    public async Task<IEnumerable<ProductPriceForDistributorDto>> GetForCategoryAsync(string category, CancellationToken ct = default)
    {
        var products = await _context.Products
            .AsNoTracking()
            .Where(p => p.IsActive && !p.IsDeleted)
            .OrderBy(p => p.Code)
            .ToListAsync(ct);

        var productIds = products.Select(p => p.Id).ToList();

        var pricingRows = await _context.ProductCategoryPrices
            .AsNoTracking()
            .Where(x => productIds.Contains(x.ProductId) && x.Category == category)
            .ToListAsync(ct);

        var priceByProduct = pricingRows.ToDictionary(x => x.ProductId, x => x.Price);

        return products.Select(p => new ProductPriceForDistributorDto(
            ProductId: p.Id,
            ProductCode: p.Code,
            ItemDescription: p.ItemDescription,
            UnitPrice: priceByProduct.GetValueOrDefault(p.Id, 0m)
        ));
    }

    public async Task BulkUpsertAsync(IEnumerable<PricingRowRequest> items, int callerId, CancellationToken ct = default)
    {
        var itemList = items.ToList();
        var productIds = itemList.Select(i => i.ProductId).Distinct().ToList();

        // Reject unknown products up-front so we return a clean 400 instead of a
        // raw FK-violation (500) on SaveChanges.
        var existingProductIds = await _context.Products
            .Where(p => productIds.Contains(p.Id))
            .Select(p => p.Id)
            .ToListAsync(ct);
        var missing = productIds.Except(existingProductIds).ToList();
        if (missing.Count > 0)
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["items"] = [$"Unknown product ID(s): {string.Join(", ", missing)}."]
            });

        // Load all existing price rows for the affected products in one query
        var existingRows = await _context.ProductCategoryPrices
            .Where(x => productIds.Contains(x.ProductId))
            .ToListAsync(ct);

        var existingLookup = existingRows
            .ToDictionary(x => (x.ProductId, x.Category));

        var now = DateTime.UtcNow;

        foreach (var item in itemList)
        {
            var categoryPrices = new Dictionary<string, decimal>
            {
                ["A"] = item.PriceA,
                ["B"] = item.PriceB,
                ["C"] = item.PriceC,
                ["D"] = item.PriceD,
            };

            foreach (var cat in Categories)
            {
                var price = categoryPrices[cat];

                if (existingLookup.TryGetValue((item.ProductId, cat), out var existing))
                {
                    existing.Price = price;
                    existing.UpdatedAt = now;
                    existing.UpdatedBy = callerId;
                    _context.ProductCategoryPrices.Update(existing);
                }
                else
                {
                    _context.ProductCategoryPrices.Add(new ProductCategoryPrice
                    {
                        ProductId = item.ProductId,
                        Category = cat,
                        Price = price,
                        CreatedAt = now,
                        UpdatedAt = now,
                        CreatedBy = callerId,
                        UpdatedBy = callerId,
                    });
                }
            }
        }

        await _context.SaveChangesAsync(ct);
    }
}
