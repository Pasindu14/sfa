# API Architecture — sfa_api

## Vertical Slice Layout

Each feature lives under `Features/{Feature}/`:

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

**Pattern:** Controller → Service → Repository — no CQRS, no MediatR.

Service extensions (`{Feature}ServiceExtensions.cs`) register all DI in `Program.cs`.

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
    object? CurrentData,                  // Optimistic locking conflicts
    string? TraceId,
    DateTime Timestamp
);
```

Always use `ResponseHelper.Ok(data, correlationId)` in controllers — never construct manually.

## EF Core Conventions

- `AsNoTracking()` on every read query — never omit on list/get endpoints
- `SaveChangesAsync(cancellationToken)` — always pass cancellation token
- Never call `context.Remove()` — soft-delete: set `IsActive = false`, `IsDeleted = true`
- `AuditInterceptor` auto-populates `CreatedAt`, `CreatedBy`, `UpdatedAt`, `UpdatedBy` — never set manually
- Optimistic concurrency via `RowVersion` on entities that need it
- All queries must have `Take()` — no unbounded lists

## Infrastructure Services

| Service | Interface | Usage |
|---------|-----------|-------|
| Redis cache | `ICacheService` | All reference data — geo hierarchy, products, pricing |
| Distributed lock | `IDistributedLockService` | Concurrent operation prevention (PostgreSQL advisory locks) |
| Idempotency | `IIdempotencyService` | POST endpoints with `Idempotency-Key` header |
| Token revocation | `ITokenRevocationService` | Logout / token blacklisting |

All infra services are injected via constructor — never instantiated directly.

## Exception → HTTP Status Mapping

| Exception | HTTP Status |
|-----------|-------------|
| `NotFoundException` | 404 |
| `ValidationException` (FluentValidation) | 422 |
| `ConflictException` | 409 |
| `UnauthorizedException` | 401 |
| `ForbiddenException` | 403 |
| Unhandled | 500 (no stack trace or raw message exposed) |

Handled by `GlobalExceptionMiddleware` — never catch-and-return in controllers.

## Scale Rules

- 500 field reps, single company
- ~50 concurrent users, ~200 req/min peak
- Cursor-based pagination on large tables — no `OFFSET` on high-row-count queries
- `AsNoTracking()` mandatory on all read queries
- Redis cache mandatory on all reference data (catalogs, price lists, geo hierarchy)
- `Take()` required on every list — no unbounded queries
- Composite indexes on every column combination used in `WHERE` + `ORDER BY`
- All timestamps: `timestamptz`, use `DateTime.UtcNow` only — never `DateTime.Now`
- Connection pool: `MaxPoolSize=100`, `MinPoolSize=10`
