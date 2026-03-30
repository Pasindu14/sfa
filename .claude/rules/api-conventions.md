---
description: .NET API-specific conventions — exception mapping, EF Core patterns, auth, infrastructure services. Loaded only when editing sfa_api files.
paths:
  - "sfa_api/**"
---

# API Conventions — sfa_api

## Exception Types → HTTP Status

| Exception                     | HTTP |
|-------------------------------|------|
| `ValidationException`         | 400  |
| `AuthenticationException`     | 401  |
| `TokenExpiredException`       | 401  |
| `AuthorizationException`      | 403  |
| `NotFoundException`           | 404  |
| `DuplicateResourceException`  | 409  |
| `ConcurrencyConflictException`| 409  |
| `BusinessRuleException`       | 422  |
| `RateLimitException`          | 429  |
| `InfrastructureException`     | 503  |

Throw these from services — `GlobalExceptionMiddleware` catches and formats them.

---

## EF Core & Database

- **Database:** PostgreSQL (Npgsql.EntityFrameworkCore)
- **DbContext:** `AppDbContext` at `Infrastructure/Persistence/AppDbContext.cs`
- **Audit:** Interceptor automatically sets `CreatedAt`, `UpdatedAt`, `CreatedBy`, `UpdatedBy`
- **Soft delete / deactivation:** Set `IsActive = false` — never call `context.Remove()`. Filter active records with `.Where(x => x.IsActive)`. All entities have `IsDeleted = true` set by the DELETE endpoint as an audit flag to distinguish deletion from deactivation. `IsActive` is the universal status field present on all entities; `IsDeleted` is the audit flag.
- **Decimal columns:** Use `[Column(TypeName = "decimal(5,2)")]` for percentage/rate fields
- **Migrations:**
  ```bash
  dotnet ef migrations add <Name> --project "d:\Github\sfa\sfa_api\sfa_api\sfa_api.csproj"
  dotnet ef database update --project "d:\Github\sfa\sfa_api\sfa_api\sfa_api.csproj"
  ```

---

## Authentication & JWT

- **Login:** POST `/api/v1/auth/login` → returns `AuthResponseDto`
  ```json
  { "accessToken": "...", "refreshToken": "...", "accessTokenExpiry": "...",
    "refreshTokenExpiry": "...", "user": { "id", "name", "email", "role" } }
  ```
- **Token rotation:** On refresh, old token consumed; reuse detection revokes entire family
- **Device binding:** Refresh tokens are locked to device via `DeviceId` claim
- **Hash storage:** Refresh tokens stored as BCrypt hash, never plain
- **Roles:** `Admin`, `SalesRep`, `Manager`

---

## Infrastructure Services

| Interface                   | Implementation                    | Purpose                          |
|-----------------------------|-----------------------------------|----------------------------------|
| `ICacheService`             | `MemoryCacheService`              | Short-lived in-memory cache      |
| `IIdempotencyService`       | `PostgresIdempotencyService`      | Prevent duplicate API calls      |
| `ITokenRevocationService`   | `PostgresTokenRevocationService`  | Track revoked/expired tokens     |
| `IDistributedLockService`   | `PostgresAdvisoryLockService`     | Prevent concurrent modifications |

---

## API-Specific Never Do

- Never omit `.Where(x => x.IsActive)` in repository queries that return active records — there is no global query filter
- Never skip FluentValidation — all request inputs must have a registered validator
- Never use PUT for partial updates — PUT replaces the full resource; use PATCH only if already in the feature
