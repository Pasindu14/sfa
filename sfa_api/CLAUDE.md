# CLAUDE.md — sfa_api (.NET 8)

## How to Run

```bash
dotnet run --project "d:\Github\sfa\sfa_api\sfa_api\sfa_api.csproj"
dotnet test --project "d:\Github\sfa\sfa_api\sfa_api\sfa_api.csproj"
```

Swagger UI: `http://localhost:5135/swagger` (http profile) or `https://localhost:7169/swagger` (https profile)

**Dev Access Token (never expires):**
```
eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIzIiwiZW1haWwiOiJhZG1pbkBzZmEuY29tIiwianRpIjoiMWM5MWJlOTgtMTIxZC00YmQ2LWJiMzEtODc2ZjA2OWY1Nzc0IiwibmFtZSI6ImFkbWluIiwiaHR0cDovL3NjaGVtYXMubWljcm9zb2Z0LmNvbS93cy8yMDA4LzA2L2lkZW50aXR5L2NsYWltcy9yb2xlIjoiQWRtaW4iLCJkZXZpY2VJZCI6IiIsImV4cCI6MTgwNDQ4MzMwNCwiaXNzIjoiU0ZBLkFQSSIsImF1ZCI6IlNGQS5DbGllbnRzIn0.Uw_CFCZQQ5Fw4v0Ctv58-g8lsu1rRWvG1n09T1RzdX4
```
Claims: `sub=3`, `email=admin@sfa.com`, `name=admin`, `role=Admin`

---

## Directory Layout

```
sfa_api/sfa_api/
├── Program.cs                         ← DI registration + middleware pipeline
├── Features/{Feature}/                ← vertical slices (see Feature Architecture below)
│   Each has: Controllers/, DTOs/, Entities/, Repositories/, Requests/, Services/, Validators/
├── Common/
│   ├── Errors/                        ← ApiResponse, SFAException, ResponseHelper
│   ├── Middleware/                     ← GlobalExceptionMiddleware, CorrelationIdMiddleware
│   ├── Extensions/                    ← Cors, JWT, RateLimit, Swagger, HealthCheck extensions
│   └── Audit/                         ← AuditInterceptor, IdempotencyKey, RevokedToken
├── Infrastructure/
│   ├── Persistence/                   ← AppDbContext, DataSeeder, DesignTimeDbContextFactory
│   ├── Caching/                       ← ICacheService, IIdempotencyService, ITokenRevocationService
│   ├── Locking/                       ← IDistributedLockService (PostgreSQL advisory locks)
│   └── Logging/                       ← SerilogConfig
└── Migrations/
```

### Implemented Features
| Feature      | Description                                             |
|--------------|---------------------------------------------------------|
| Auth         | Login, JWT refresh, logout, token revocation            |
| Users        | User CRUD, password change, status toggle               |
| Distributors | Distributor CRUD                                        |
| Regions      | Region CRUD, activate/deactivate                        |
| Areas        | Area CRUD, activate/deactivate — stores `RegionId`      |
| Territories  | Territory CRUD, activate/deactivate — stores `AreaId` + `RegionId` (denormalized) |

### Geographic Hierarchy
```
Region → Area → Territory → Division (planned)
```
Each level stores all ancestor IDs directly (denormalized) for flat, join-free queries.
See `dotnet-feature-generator` skill for the full denormalization pattern.

Test projects: `sfa_api.IntegrationTests/`, `sfa_api.UnitTests/`

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
