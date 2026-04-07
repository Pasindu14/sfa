---
name: SFA API Project Context
description: Key architectural facts about sfa_api that affect audit judgements
type: project
---

Key facts discovered during audit sessions:

- No CQRS/MediatR — pattern is Controller → Service → Repository.
- No EF Core global query filter in use universally: some entities (Region, Area, Territory, Division, Route, Outlet) DO have HasQueryFilter(x => x.IsActive) configured. However, repos call IgnoreQueryFilters() and filter manually. Infrastructure tables (User, Distributor, Product, PricingStructure) deliberately omit global query filters due to cascade/navigation concerns.
- Idempotency is opt-in via `X-Idempotency-Key` header — the middleware is global but silently skips requests that omit the header. No [RequireIdempotencyKey] attribute exists.
- Rate limiting is IP-based global sliding window (100 req/60s). A "user" per-user fixed window policy exists but Areas write endpoints do NOT apply it via [EnableRateLimiting("user")]. Only the global IP limiter applies.
- AuditInterceptor auto-captures CREATE/UPDATE/DELETE audit rows on every SaveChanges call — Areas benefit from this automatically.
- appsettings.json contains a live PostgreSQL connection string and JWT secret committed to git — critical security finding.
- Middleware order in Program.cs: ResponseCompression → CorrelationId → SerilogRequestLogging → GlobalExceptionMiddleware → HttpsRedirection → CORS → ForwardedHeaders → RateLimiter → Authentication → Authorization → IdempotencyMiddleware. GlobalExceptionMiddleware is placed AFTER Serilog request logging — unhandled exceptions are logged by Serilog before being caught.
- DELETE endpoint (soft-delete via IsDeleted) is missing from Areas controller; only activate/deactivate exists.
- Areas migration is missing the IsDeleted column — it was added to the entity but the migration only creates IsActive, Name, RegionId columns. IsDeleted filter in DbContext will cause runtime errors if the column doesn't exist in DB.
- Areas migration FK is Cascade delete (FK_Areas_Regions_RegionId) but DbContext configures it as Required with no explicit OnDelete — EF default is Cascade, which is dangerous for region deletion cascading to all child areas.
- GetByIdAsync in AreaRepository has no AsNoTracking() — it returns a tracked entity for both reads and writes; acceptable for mutation paths but the service's GetByIdAsync (used by the controller as a read-only endpoint) goes through the same tracked path.
- GetAllActiveAsync in AreaRepository has no Take() bound — can return all active areas in a single query with no pagination limit.
- Cache invalidation is missing from AreaService — CreateAsync/UpdateAsync/ActivateAsync/DeactivateAsync never call _cache.RemoveAsync, so stale list cache persists for 5 minutes after writes.
- AreaService.GetAllActiveAsync does not cache results even though it queries all active areas.
- ICacheService.GetAsync/SetAsync are not CancellationToken-aware — ct is not propagated into cache operations.
