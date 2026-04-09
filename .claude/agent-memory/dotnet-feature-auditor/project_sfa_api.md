---
name: SFA API Project Context
description: Key architectural facts about sfa_api that affect audit judgements
type: project
---

Key facts discovered during audit sessions:

- No CQRS/MediatR — pattern is Controller → Service → Repository.
- No EF Core global query filter in use universally: some entities (Region, Area, Territory, Division, Route, Outlet) DO have HasQueryFilter(x => x.IsActive) configured. However, repos call IgnoreQueryFilters() and filter manually. Infrastructure tables (User, Distributor, Product, PricingStructure) deliberately omit global query filters due to cascade/navigation concerns.
- Idempotency is opt-in via `X-Idempotency-Key` header — the middleware is global but silently skips requests that omit the header. No [RequireIdempotencyKey] attribute exists.
- Rate limiting: global IP-based sliding window (100 req/60s) + named "user" per-user fixed window. Areas write endpoints DO apply [EnableRateLimiting("user")] on all POST/PUT/DELETE actions.
- AuditInterceptor auto-captures CREATE/UPDATE/DELETE audit rows on every SaveChanges call — Areas benefit from this automatically.
- appsettings.json has EMPTY placeholder values for connection string and JWT secret (confirmed 2026-04-08) — not committed secrets. No appsettings.Development.json exists.
- Middleware order in Program.cs: ResponseCompression → GlobalExceptionMiddleware → CorrelationId → SerilogRequestLogging → HttpsRedirection → CORS → ForwardedHeaders → RequestTimeouts → RateLimiter → Authentication → Authorization → IdempotencyMiddleware. Note: GlobalExceptionMiddleware is placed AFTER ResponseCompression but BEFORE Serilog — exceptions are caught before Serilog's per-request log line runs, so Serilog sees the error status correctly.
- DELETE endpoint for Areas is implemented: soft-deletes via IsDeleted=true + IsActive=false. Found in AreasController.cs line 141.
- Areas migration history: initial migration (AddAreaEntity) had no IsDeleted column and used Cascade FK. Two fix migrations exist: AddAreaIsDeletedColumn (adds IsDeleted boolean) and AlterAreaRegionFkToRestrict (changes FK to Restrict). Both look correct.
- GetByIdAsync in AreaRepository has AsNoTracking(). GetByIdTrackedAsync exists as separate method for mutation paths. Pattern is correct.
- GetAllActiveAsync now has Take(1000) bound.
- Cache invalidation is present: CreateAsync/UpdateAsync/ActivateAsync/DeactivateAsync/DeleteAsync all call RemoveByPrefixAsync(ActiveCacheKey) and RemoveByPrefixAsync(ListCachePrefix).
- GetAllAsync caches results with SetAsync.
- ICacheService.GetAsync/SetAsync take a CancellationToken — ct IS passed to cache calls.
- RowVersion on Area maps to PostgreSQL xmin column via IsRowVersion(). ApplyConcurrencyToken method in AreaRepository correctly sets context.Entry(area).Property(x => x.RowVersion).OriginalValue = rowVersion. This was fixed in a prior session — the previous bug of assigning area.RowVersion directly is gone.
- Connection string in appsettings.json lacks MaxPoolSize/MinPoolSize — these are ADO.NET pool settings for Npgsql, not EF options; they must be in the connection string itself. Still not set.
- RequestTimeouts middleware IS registered (UseRequestTimeouts, 30s default policy) — resiliency #10 passes.
- EnableRetryOnFailure(3) is configured on EF/Npgsql — covers DB transient failures. No Polly wrapping for Redis/cache calls.
- Areas is reference data (<500 rows expected) — offset pagination (Skip/Take) is appropriate. No cursor needed.
- DbUpdateConcurrencyException IS caught in SaveChangesAsync (AreaRepository line 107–113) and rethrown as ConcurrencyConflictException — this was fixed in a prior session.
- GetAllActiveAsync (AreaRepository line 55) uses global HasQueryFilter(x => x.IsActive) but does NOT add a Where(!a.IsDeleted) guard. The HasQueryFilter only covers IsActive. In practice safe because DeleteAsync always sets IsActive=false, but no explicit IsDeleted guard. Still unfixed.
- RegionExistsAsync (AreaRepository line 85) calls IgnoreQueryFilters() with no IsDeleted or IsActive filter — allows creating an Area under an inactive/deleted Region. Still unfixed.
- page query param in GetAll endpoint: repo does Math.Clamp(take, 1, 200) but no lower-bound guard on page/skip. page=0 produces skip=-pageSize which PostgreSQL rejects. Still unfixed in AreaService.GetAllAsync.
- FixAreaIndexes migration (20260408050138) added IX_Areas_RegionId_IsActive_IsDeleted composite index — this is already applied.
- GetAllAsync does 2 separate queries (CountAsync + ToListAsync) which is correct; no N+1.
- No IAsyncEnumerable streaming used; ToList() on GetAllActiveAsync result — acceptable for bounded Take(1000) list.
- UpdatedAt partial index uses HasFilter("\"IsActive\" = true") — fine.
- OpenTelemetry tracing + metrics are registered (AspNetCore + EF Core instrumentation) — observability #3 metrics pass.
- AddDbContextPool IS used (not AddDbContext) — data access #8 passes.
- CommandTimeout=30 IS set on EF options.
- UseRequestTimeouts(30s) IS registered in middleware pipeline.
