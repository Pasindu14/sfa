# CLAUDE.md — sfa_api (.NET 8)

## How to Run

```bash
cd sfa_api
dotnet run --project sfa_api
dotnet test
```

Swagger UI: `https://localhost:7237/swagger`

---

## Feature Architecture (Vertical Slice)

Each feature lives under `Features/{Feature}/` with this exact layout:

```
Features/{Feature}/
├── Controllers/{Feature}Controller.cs   ← REST endpoints, [Authorize] guards
├── Entities/{Feature}.cs                ← EF Core entity with audit fields
├── DTOs/{Feature}Dto.cs                 ← Immutable records; separate List variant
├── Requests/Create{Feature}Request.cs   ← Input binding
├── Requests/Update{Feature}Request.cs
├── Repositories/I{Feature}Repository.cs ← Async interface
├── Repositories/{Feature}Repository.cs  ← EF Core LINQ queries
├── Services/I{Feature}Service.cs        ← Business logic interface
├── Services/{Feature}Service.cs         ← Calls repo, accepts callerId for audit
└── Validators/{Feature}Validator.cs     ← FluentValidation rules
```

No CQRS/MediatR. Pattern is **Controller → Service → Repository**.

---

## API Response Envelope

```csharp
// Common/Errors/ApiResponse.cs
record ApiResponse<T>(bool Success, T? Data, PaginationMeta? Pagination, string? TraceId);

// Error shape (non-2xx)
record ApiError(
    string Code,                          // e.g. "USER_NOT_FOUND"
    string Message,
    string? Detail,
    Dictionary<string, string[]>? Fields, // Validation errors per field
    object? CurrentData,                  // For optimistic locking conflicts
    string? TraceId,
    DateTime Timestamp
);
```

Use `ResponseHelper.Ok(data, correlationId)` in controllers — never construct manually.

---

## Exception Types → HTTP Status

| Exception                    | HTTP |
|------------------------------|------|
| `ValidationException`        | 400  |
| `AuthenticationException`    | 401  |
| `TokenExpiredException`      | 401  |
| `AuthorizationException`     | 403  |
| `NotFoundException`          | 404  |
| `DuplicateResourceException` | 409  |
| `ConcurrencyConflictException`| 409 |
| `BusinessRuleException`      | 422  |
| `RateLimitException`         | 429  |
| `InfrastructureException`    | 503  |

Throw these from services — `GlobalExceptionMiddleware` catches and formats them.

---

## EF Core & Database

- **Database:** PostgreSQL (Npgsql.EntityFrameworkCore)
- **DbContext:** `AppDbContext` at `Infrastructure/Persistence/AppDbContext.cs`
- **Audit:** Interceptor automatically sets `CreatedAt`, `UpdatedAt`, `CreatedBy`, `UpdatedBy`
- **Soft delete:** Set `IsDeleted = true` — no hard deletes. **No global query filter** — add `.Where(x => !x.IsDeleted)` explicitly in every query
- **Decimal columns:** Use `[Column(TypeName = "decimal(5,2)")]` for percentage/rate fields
- **Migrations:**
  ```bash
  dotnet ef migrations add <Name> --project sfa_api
  dotnet ef database update --project sfa_api
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

| Interface                   | Implementation                    | Purpose                         |
|-----------------------------|-----------------------------------|---------------------------------|
| `ICacheService`             | `MemoryCacheService`              | Short-lived in-memory cache     |
| `IIdempotencyService`       | `PostgresIdempotencyService`      | Prevent duplicate API calls     |
| `ITokenRevocationService`   | `PostgresTokenRevocationService`  | Track revoked/expired tokens    |
| `IDistributedLockService`   | `PostgresAdvisoryLockService`     | Prevent concurrent modifications|

---

## Never Do

- Never hard-delete — always soft-delete (`IsDeleted = true`)
- Never omit `.Where(x => !x.IsDeleted)` in repository queries
- Never expose raw exception messages or stack traces
- Never use SQL Server — database is PostgreSQL
- Never send or accept tenant/company ID from the client — resolve from JWT claims
- Never skip FluentValidation — all request inputs must have a validator
- Never use PUT for partial updates — PUT replaces the full resource; no PATCH unless already used in the feature
