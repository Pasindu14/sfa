# CLAUDE.md — sfa_api (.NET 8)

## How to Run

```bash
dotnet run --project "d:\Github\sfa\sfa_api\sfa_api\sfa_api.csproj"
dotnet test "d:\Github\sfa\sfa_api\sfa_api.UnitTests\sfa_api.UnitTests.csproj"
dotnet test "d:\Github\sfa\sfa_api\sfa_api.IntegrationTests\sfa_api.IntegrationTests.csproj"
```

Swagger: `http://localhost:5135/swagger` (http) | `https://localhost:7169/swagger` (https)

**Dev Token (never expires):**
```
eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIzIiwiZW1haWwiOiJhZG1pbkBzZmEuY29tIiwianRpIjoiMWM5MWJlOTgtMTIxZC00YmQ2LWJiMzEtODc2ZjA2OWY1Nzc0IiwibmFtZSI6ImFkbWluIiwiaHR0cDovL3NjaGVtYXMubWljcm9zb2Z0LmNvbS93cy8yMDA4LzA2L2lkZW50aXR5L2NsYWltcy9yb2xlIjoiQWRtaW4iLCJkZXZpY2VJZCI6IiIsImV4cCI6MTgwNDQ4MzMwNCwiaXNzIjoiU0ZBLkFQSSIsImF1ZCI6IlNGQS5DbGllbnRzIn0.Uw_CFCZQQ5Fw4v0Ctv58-g8lsu1rRWvG1n09T1RzdX4
```
Claims: `sub=3`, `email=admin@sfa.com`, `name=admin`, `role=Admin`

---

## Directory Layout

```
sfa_api/sfa_api/
├── Program.cs                         ← DI + middleware pipeline
├── Features/{Feature}/                ← vertical slices
│   Each: Controllers/ DTOs/ Entities/ Repositories/ Requests/ Services/ Validators/
├── Common/
│   ├── Errors/                        ← ApiResponse, SFAException, ResponseHelper
│   ├── Middleware/                    ← GlobalExceptionMiddleware, CorrelationIdMiddleware
│   ├── Extensions/                   ← Cors, JWT, RateLimit, Swagger, HealthCheck
│   └── Audit/                        ← AuditInterceptor, IdempotencyKey, RevokedToken
├── Infrastructure/
│   ├── Persistence/                   ← AppDbContext, DataSeeder
│   ├── Caching/                       ← ICacheService, IIdempotencyService, ITokenRevocationService
│   ├── Locking/                       ← IDistributedLockService (PostgreSQL advisory locks)
│   └── Logging/                       ← SerilogConfig
└── Migrations/

Test: sfa_api.UnitTests/ | sfa_api.IntegrationTests/
```

---

## Implemented Features

> Full descriptions and domain notes → @.claude/docs/api-features.md

| Auth | Users | Distributors | Regions | Areas |
|------|-------|--------------|---------|-------|
| Territories | Divisions | Outlets | Products | Categories |
| ProductCategories | ProductCategoryPricings | PricingStructures | PurchaseOrders | SalesInvoices |
| GRNs | Billings | NotBillings | Routes | DailyRouteAssignments |
| Stock | Fleets | UserGeoAssignments | UserReportingLines | MobileSync |

**Geographic Hierarchy:** `Region → Area → Territory → Division`
Each level stores all ancestor IDs (denormalized) — join-free queries.

---

## Architecture & Patterns

> Vertical slice layout, EF Core conventions, scale rules → @.claude/docs/api-architecture.md

- **Pattern:** Controller → Service → Repository (no CQRS/MediatR)
- **Response:** `ResponseHelper.Ok(data, correlationId)` — never construct manually
- **Envelope:** `ApiResponse<T>` — `{ success, data, pagination, traceId }`
- **Errors:** `ApiError` — `{ code, message, fields, traceId }` — never expose stack traces
- **Scale:** 500 reps, ~50 concurrent, ~200 req/min — Redis + `AsNoTracking()` mandatory

> Exception → HTTP status mapping, EF Core patterns, auth details, infra services
> auto-loaded via `.claude/rules/api-conventions.md` when editing `sfa_api/**`.
