# Example: Category Feature (Simple CRUD)

Demonstrates the minimal feature — no hierarchies, no batch ops, no locking.

**References loaded:** None (simple CRUD only needs the base skill).

---

## Requirements

| Field | Value |
|-------|-------|
| Entity name | `Category` |
| Route segment | `categories` |
| DbSet name | `Categories` |
| High-growth? | No |
| Hierarchical? | No |

### Properties

| Property | C# Type | Nullable | Default |
|----------|---------|----------|---------|
| Id | int | No | identity |
| Name | string | No | `= string.Empty` |
| Description | string? | Yes | null |
| CreatedAt | DateTime | No | `= DateTime.UtcNow` |
| UpdatedAt | DateTime | No | `= DateTime.UtcNow` |
| CreatedBy | int? | Yes | null |
| UpdatedBy | int? | Yes | null |
| IsActive | bool | No | `= true` |
| IsDeleted | bool | No | `= false` |

### Validation

| Field | Rule | Message |
|-------|------|---------|
| Name | NotEmpty | "Name is required." |
| Name | MaximumLength(100) | "Name must not exceed 100 characters." |
| Name | Unique (ExistsByNameAsync) | "Category name already exists." |
| Description | MaximumLength(500) | "Description must not exceed 500 characters." |

### Endpoints

| Method | Route | Auth |
|--------|-------|------|
| GET | /api/v1/categories | `[Authorize]` |
| GET | /api/v1/categories/{id} | `[Authorize]` |
| POST | /api/v1/categories | `[Authorize(Roles="Admin")]` |
| PUT | /api/v1/categories/{id} | `[Authorize(Roles="Admin")]` |
| DELETE | /api/v1/categories/{id} | `[Authorize(Roles="Admin")]` |

---

## Generated Files

11 files created + 2 existing files edited:

```
sfa_api/sfa_api/Features/Category/
├── Entities/Category.cs
├── DTOs/CategoryDto.cs
├── Requests/CreateCategoryRequest.cs
├── Requests/UpdateCategoryRequest.cs
├── Validators/CreateCategoryValidator.cs
├── Validators/UpdateCategoryValidator.cs
├── Repositories/ICategoryRepository.cs
├── Repositories/CategoryRepository.cs
├── Services/ICategoryService.cs
├── Services/CategoryService.cs
└── Controllers/CategoriesController.cs

Edited:
  AppDbContext.cs — DbSet + OnModelCreating
  Program.cs     — 4 Scoped registrations
```
