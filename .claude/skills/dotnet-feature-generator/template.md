# Feature Generation Plan: {FeatureName}

Fill in every section before generating any code.

---

## Entity Summary

| Field       | Value |
|-------------|-------|
| Feature name (singular PascalCase) | `{FeatureName}` |
| Route segment (lowercase plural)   | `{entities}` |
| DbSet name                         | `{Entities}` |
| Namespace prefix                   | `sfa_api.Features.{FeatureName}` |

---

## Properties

| Property | C# Type | Nullable | Default | Notes |
|----------|---------|----------|---------|-------|
| Id       | int     | No       | identity | PK, auto-increment |
| Name     | string  | No       | `= string.Empty` | |
| ...      |         |          |         | |
| CreatedAt | DateTime | No | `= DateTime.UtcNow` | audit |
| UpdatedAt | DateTime | No | `= DateTime.UtcNow` | audit |
| CreatedBy | int?    | Yes | null | audit |
| UpdatedBy | int?    | Yes | null | audit |
| IsActive  | bool   | No  | `= true` | soft-delete flag |

---

## Enums (if any)

```csharp
public enum {FeatureName}Status { Active, Inactive }
```

---

## Validation Rules

| Field | Rule | Error message |
|-------|------|---------------|
| Name  | NotEmpty | "Name is required." |
| Name  | MaximumLength(100) | "Name must not exceed 100 characters." |
| ...   | | |

---

## Endpoints

| Method | Route                          | Auth                    | Notes |
|--------|-------------------------------|-------------------------|-------|
| GET    | /api/v1/{entities}            | `[Authorize]`           | paginated, search |
| GET    | /api/v1/{entities}/{id}       | `[Authorize]`           | |
| POST   | /api/v1/{entities}            | `[Authorize(Roles="Admin")]` | |
| PUT    | /api/v1/{entities}/{id}       | `[Authorize(Roles="Admin")]` | |
| DELETE | /api/v1/{entities}/{id}       | `[Authorize(Roles="Admin")]` | soft delete |

---

## Files to Generate

```
sfa_api/sfa_api/Features/{FeatureName}/
├── Entities/
│   └── {FeatureName}.cs
├── DTOs/
│   ├── {FeatureName}Dto.cs
│   └── {FeatureName}ListDto.cs
├── Requests/
│   ├── Create{FeatureName}Request.cs
│   └── Update{FeatureName}Request.cs
├── Validators/
│   ├── Create{FeatureName}Validator.cs
│   └── Update{FeatureName}Validator.cs
├── Repositories/
│   ├── I{FeatureName}Repository.cs
│   └── {FeatureName}Repository.cs
├── Services/
│   ├── I{FeatureName}Service.cs
│   └── {FeatureName}Service.cs
└── Controllers/
    └── {FeatureName}sController.cs
```

Plus edits to:
- `sfa_api/sfa_api/Infrastructure/Persistence/AppDbContext.cs`
- `sfa_api/sfa_api/Program.cs`

---

## Migration Command

```bash
cd sfa_api/sfa_api
dotnet ef migrations add Add{FeatureName}Entity --project . --startup-project .
```
