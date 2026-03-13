# Example: Category Feature

A minimal but complete feature — `name` (unique, required) and `description` (optional). All endpoints Admin-only.

---

## Entities/Category.cs

```csharp
namespace sfa_api.Features.Categories.Entities;

public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public int? CreatedBy { get; set; }
    public int? UpdatedBy { get; set; }
    public bool IsActive { get; set; } = true;
}
```

---

## Repositories/ICategoryRepository.cs

```csharp
using sfa_api.Features.Categories.Entities;

namespace sfa_api.Features.Categories.Repositories;

public interface ICategoryRepository
{
    Task<Category?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<(IEnumerable<Category>, int TotalCount)> GetAllAsync(int skip, int take, string? search = null, CancellationToken ct = default);
    Task<bool> ExistsByNameAsync(string name, CancellationToken ct = default);
    Task<bool> ExistsByNameAsync(string name, int excludeId, CancellationToken ct = default);
    Task CreateAsync(Category entity, CancellationToken ct = default);
    Task UpdateAsync(Category entity, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
```

---

## Repositories/CategoryRepository.cs

```csharp
using Microsoft.EntityFrameworkCore;
using sfa_api.Features.Categories.Entities;
using sfa_api.Infrastructure.Persistence;

namespace sfa_api.Features.Categories.Repositories;

public class CategoryRepository(AppDbContext context) : ICategoryRepository
{
    private readonly AppDbContext _context = context;

    public async Task<Category?> GetByIdAsync(int id, CancellationToken ct = default)
        => await _context.Categories.FindAsync([id], ct);

    public async Task<(IEnumerable<Category>, int)> GetAllAsync(int skip, int take, string? search = null, CancellationToken ct = default)
    {
        var query = _context.Categories.Where(x => x.IsActive);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(x => x.Name.ToLower().Contains(search.ToLower()));

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(x => x.Name)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task<bool> ExistsByNameAsync(string name, CancellationToken ct = default)
        => await _context.Categories.AnyAsync(x => x.Name == name && x.IsActive, ct);

    public async Task<bool> ExistsByNameAsync(string name, int excludeId, CancellationToken ct = default)
        => await _context.Categories.AnyAsync(x => x.Name == name && x.Id != excludeId && x.IsActive, ct);

    public async Task CreateAsync(Category entity, CancellationToken ct = default)
        => await _context.Categories.AddAsync(entity, ct);

    public Task UpdateAsync(Category entity, CancellationToken ct = default)
    {
        _context.Categories.Update(entity);
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var entity = await _context.Categories.FindAsync([id], ct);
        if (entity != null)
        {
            entity.IsActive = false;
            _context.Categories.Update(entity);
        }
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await _context.SaveChangesAsync(ct);
}
```

---

## DTOs/CategoryDto.cs

```csharp
namespace sfa_api.Features.Categories.DTOs;

public record CategoryDto(
    int Id,
    string Name,
    string? Description,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record CategoryListDto(
    IEnumerable<CategoryDto> Categories,
    int TotalCount,
    int Page,
    int PageSize
);
```

---

## Requests/CreateCategoryRequest.cs

```csharp
namespace sfa_api.Features.Categories.Requests;

public class CreateCategoryRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}
```

---

## Requests/UpdateCategoryRequest.cs

```csharp
namespace sfa_api.Features.Categories.Requests;

public class UpdateCategoryRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}
```

---

## Validators/CreateCategoryValidator.cs

```csharp
using FluentValidation;
using sfa_api.Features.Categories.Requests;

namespace sfa_api.Features.Categories.Validators;

public class CreateCategoryValidator : AbstractValidator<CreateCategoryRequest>
{
    public CreateCategoryValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(100).WithMessage("Name must not exceed 100 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description must not exceed 500 characters.")
            .When(x => x.Description != null);
    }
}
```

---

## Services/ICategoryService.cs

```csharp
using sfa_api.Features.Categories.DTOs;
using sfa_api.Features.Categories.Requests;

namespace sfa_api.Features.Categories.Services;

public interface ICategoryService
{
    Task<CategoryDto> GetByIdAsync(int id, CancellationToken ct = default);
    Task<CategoryListDto> GetAllAsync(int page, int pageSize, string? search = null, CancellationToken ct = default);
    Task<CategoryDto> CreateAsync(CreateCategoryRequest request, int? callerId, CancellationToken ct = default);
    Task<CategoryDto> UpdateAsync(int id, UpdateCategoryRequest request, int? callerId, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}
```

---

## Program.cs additions

```csharp
// ── Category Feature ──────────────────────────────────────────────────
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IValidator<CreateCategoryRequest>, CreateCategoryValidator>();
builder.Services.AddScoped<IValidator<UpdateCategoryRequest>, UpdateCategoryValidator>();
```

---

## AppDbContext additions

```csharp
public DbSet<Category> Categories => Set<Category>();
```

```csharp
modelBuilder.Entity<Category>(e =>
{
    e.HasKey(x => x.Id);
    e.Property(x => x.Id).UseIdentityColumn();
    e.HasIndex(x => x.Name).IsUnique();
    e.HasIndex(x => x.IsActive);
    e.HasIndex(x => x.UpdatedAt);
});
```
