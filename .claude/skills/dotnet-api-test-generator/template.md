# Test Generation Plan: {Feature}

Fill in each section before writing any test code.

---

## Decision: What to Generate

| Test type | Generate? | Reason |
|-----------|-----------|--------|
| Unit — `{Feature}ServiceTests` | Yes / No | Service has business logic / thin CRUD |
| Unit — `Create{Feature}ValidatorTests` | Yes / No | Validator has non-trivial rules |
| Unit — `Update{Feature}ValidatorTests` | Yes / No | Validator has non-trivial rules |
| Integration — `{Feature}sApiTests` | Yes / No | Always for endpoints |

---

## Source Files to Read First

```
sfa_api/sfa_api/Features/{Feature}/Services/{Feature}Service.cs
sfa_api/sfa_api/Features/{Feature}/Repositories/I{Feature}Repository.cs
sfa_api/sfa_api/Features/{Feature}/Validators/Create{Feature}Validator.cs
sfa_api/sfa_api/Features/{Feature}/Controllers/{Feature}sController.cs
```

---

## Unit Test File Paths

```
sfa_api/sfa_api.UnitTests/Features/{Feature}/Services/{Feature}ServiceTests.cs
sfa_api/sfa_api.UnitTests/Features/{Feature}/Validators/Create{Feature}ValidatorTests.cs
sfa_api/sfa_api.UnitTests/Features/{Feature}/Validators/Update{Feature}ValidatorTests.cs
```

---

## Integration Test File Path

```
sfa_api/sfa_api.IntegrationTests/Features/{Feature}/{Feature}sApiTests.cs
```

---

## Service Methods & Test Cases

| Method | Scenarios to test |
|--------|------------------|
| `GetByIdAsync` | existing id → returns DTO; missing id → NotFoundException |
| `GetAllAsync` | returns list with correct pagination; empty result |
| `CreateAsync` | valid → DTO + audit fields set + SaveChanges called; duplicate → DuplicateResourceException |
| `UpdateAsync` | valid → updated DTO; missing → NotFoundException; duplicate → DuplicateResourceException |
| `DeleteAsync` | existing → repo + SaveChanges called; missing → NotFoundException |

---

## Hierarchical Entity Check

Is this entity a Territory or Division (has ancestor IDs)?
- [ ] Yes → use `GetAreaWithRegionAsync` mock (returns `Area?`), NOT `AreaExistsAsync`
- [ ] No → standard uniqueness check pattern

---

## Integration Test Scenarios

| Scenario | Expected status |
|----------|----------------|
| GET /api/v1/{entities} without token | 401 |
| GET /api/v1/{entities} as SalesRep (if Admin-only) | 403 |
| GET /api/v1/{entities} as Admin | 200 + envelope |
| POST with invalid data | 400 + VALIDATION_FAILED |
| POST with valid data | 201 |
| GET /{id} of created entity | 200 + correct fields |
| PUT with update | 200 |
| DELETE | 204 |
| GET /{id} after delete | 404 |
| POST with duplicate unique field | 409 |

---

## Available Tokens

```csharp
AuthHelper.AdminToken    // role=Admin
AuthHelper.ManagerToken  // role=Manager
AuthHelper.SalesRepToken // role=SalesRep
```
