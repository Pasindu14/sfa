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
- RowVersion on Area maps to PostgreSQL xmin column via IsRowVersion(). UpdateAsync sets area.RowVersion = request.RowVersion BEFORE calling Update/SaveChanges — this is a bug: assigning the client-supplied RowVersion to the tracked entity overwrites the DB-fetched xmin value; EF Core then compares the wrong value during concurrency check. RowVersion should NOT be assigned; EF reads xmin from the tracked entity automatically.
- Connection string in appsettings.json lacks MaxPoolSize/MinPoolSize — these are ADO.NET pool settings for Npgsql, not EF options; they must be in the connection string itself.
- No request timeout middleware (ASP.NET Core 8 UseRequestTimeouts or equivalent) — only DB CommandTimeout=30 exists.
- EnableRetryOnFailure(3) is configured on EF/Npgsql — covers DB transient failures. No Polly wrapping for Redis/cache calls.
- Areas is reference data (<500 rows expected) — offset pagination (Skip/Take) is appropriate. No cursor needed.
- GetAllAsync does 2 separate queries (CountAsync + ToListAsync) which is correct; no N+1.
- No IAsyncEnumerable streaming used; ToList() on GetAllActiveAsync result — acceptable for bounded Take(1000) list.
- UpdatedAt partial index uses HasFilter("\"IsActive\" = true") — this is a composite filter index for active areas only; this is fine.
