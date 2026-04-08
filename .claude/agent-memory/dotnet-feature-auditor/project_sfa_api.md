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
- appsettings.json contains a live PostgreSQL connection string and JWT secret committed to git — critical security finding, not specific to Areas.
- Middleware order in Program.cs: ResponseCompression → GlobalExceptionMiddleware → CorrelationId → SerilogRequestLogging → HttpsRedirection → CORS → ForwardedHeaders → RateLimiter → Authentication → Authorization → IdempotencyMiddleware. Note: GlobalExceptionMiddleware is placed AFTER ResponseCompression but BEFORE Serilog — exceptions are caught before Serilog's per-request log line runs, so Serilog sees the error status correctly.
- DELETE endpoint for Areas is NOW implemented: soft-deletes via IsDeleted=true + IsActive=false. Found in AreasController.cs line 141.
- Areas migration history: initial migration (AddAreaEntity) had no IsDeleted column and used Cascade FK. Two fix migrations exist: AddAreaIsDeletedColumn (adds IsDeleted boolean) and AlterAreaRegionFkToRestrict (changes FK to Restrict). Both look correct.
- GetByIdAsync in AreaRepository NOW has AsNoTracking(). GetByIdTrackedAsync exists as separate method for mutation paths. Pattern is correct.
- GetAllActiveAsync now has Take(1000) bound.
- Cache invalidation is NOW present: CreateAsync/UpdateAsync/ActivateAsync/DeactivateAsync/DeleteAsync all call RemoveAsync(ActiveCacheKey) and RemoveByPrefixAsync(ListCachePrefix).
- GetAllActiveAsync now caches results with SetAsync.
- ICacheService.GetAsync/SetAsync take a CancellationToken — ct IS passed to cache calls.
- RowVersion on Area maps to PostgreSQL xmin column via IsRowVersion(). UpdateAsync sets area.RowVersion = request.RowVersion (AreaService.cs line 109). EF Core uses the OriginalValue of the concurrency token (loaded from DB) in the WHERE clause, not the CurrentValue. Assigning request.RowVersion to area.RowVersion changes CurrentValue only — the concurrency check still fires against the freshly-loaded DB xmin, NOT the client's supplied value. To use the client's RowVersion for cross-request staleness detection, call: context.Entry(area).Property(x => x.RowVersion).OriginalValue = request.RowVersion. The current code's assignment is effectively a no-op for concurrency checking.
- Connection string in appsettings.json lacks MaxPoolSize/MinPoolSize — these are ADO.NET pool settings for Npgsql, not EF options; they must be in the connection string itself.
- No request timeout middleware (ASP.NET Core 8 UseRequestTimeouts or equivalent) — only DB CommandTimeout=30 exists.
- EnableRetryOnFailure(3) is configured on EF/Npgsql — covers DB transient failures. No Polly wrapping for Redis/cache calls.
- Areas is reference data (<500 rows expected) — offset pagination (Skip/Take) is appropriate. No cursor needed.
- DbUpdateConcurrencyException is NEVER caught in the Areas feature or globally in GlobalExceptionMiddleware. When xmin concurrency check fires (concurrent update), EF throws DbUpdateConcurrencyException which hits the 500 fallback — should be caught and rethrown as ConcurrencyConflictException.
- GetAllActiveAsync (AreaRepository line 54) uses global HasQueryFilter(x => x.IsActive) but does NOT add a Where(!a.IsDeleted) guard. The HasQueryFilter only covers IsActive. In practice safe because DeleteAsync always sets IsActive=false, but no explicit guard.
- RegionExistsAsync (AreaRepository line 84) calls IgnoreQueryFilters() with no IsDeleted or IsActive filter — allows creating an Area under an inactive/deleted Region.
- page query param in GetAll endpoint has no lower-bound guard. page=0 produces skip=-pageSize which PostgreSQL rejects with an exception (hits 500 fallback).
- FixAreaIndexes migration (20260408050138) added IX_Areas_RegionId_IsActive_IsDeleted composite index — this is already applied.
- GetAllAsync does 2 separate queries (CountAsync + ToListAsync) which is correct; no N+1.
- No IAsyncEnumerable streaming used; ToList() on GetAllActiveAsync result — acceptable for bounded Take(1000) list.
- UpdatedAt partial index uses HasFilter("\"IsActive\" = true") — this is a composite filter index for active areas only; this is fine.
