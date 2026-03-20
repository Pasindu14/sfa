# SFA Monorepo — Production Audit Issues

> **Audit Date:** 2026-03-20
> **Total Issues:** 32
> **Severity Breakdown:** 6 Critical · 8 High · 10 Medium · 8 Low

---

## Legend

| Severity    | Meaning                                                                   |
| ----------- | ------------------------------------------------------------------------- |
| 🔴 Critical | Blocks production deployment. Security breach or data loss risk.          |
| 🟠 High     | Must be fixed before or shortly after launch. Significant risk.           |
| 🟡 Medium   | Should be fixed in the near term. Affects reliability or maintainability. |
| 🔵 Low      | Improve when time permits. Affects scalability or developer experience.   |

---

## Critical Issues

---

### #001 — Hardcoded Production Secrets in Source Control -- ignore

- **Severity:** 🔴 Critical
- **Category:** Security
- **Affected Files:**
  - `sfa_api/sfa_api/appsettings.json:10` — live PostgreSQL connection string with password
  - `sfa_api/sfa_api/appsettings.json:15` — JWT signing secret key
  - `sfa_api/CLAUDE.md` — never-expiring admin dev token embedded in documentation

- **Problem:** The live database password (`npg_ScokZ26VKDRW`) and the JWT signing secret (`a8F#9kLm2PqR7tVxY4zW!6nB@3cD$5Gh`) are committed directly into the repository in plain text. Additionally, a never-expiring JWT with `role=Admin` is embedded in `CLAUDE.md`.

- **Why it matters:** Any person with repository read access — including CI runners, GitHub bots, secrets scanning tools, or any future contractor — has full database credentials and can forge valid admin JWTs indefinitely. If this repository is ever made public or the credentials are leaked via git history, the entire system is compromised with no easy remediation short of rotating every secret and rebuilding trust.

- **What should be considered:**
  - Move all secrets to environment variables or a secrets manager (Azure Key Vault, AWS Secrets Manager, HashiCorp Vault).
  - `appsettings.json` should contain only non-secret structure with empty placeholder values.
  - The JWT SecretKey must be at least 256 bits, stored only in environment config, and rotated immediately since it is now burned in git history.
  - The dev token in `CLAUDE.md` must be revoked or the JWT secret rotated to invalidate it.

---

### #002 — Default Admin Credentials Hardcoded and Logged to Console - ignore

- **Severity:** 🔴 Critical
- **Category:** Security
- **Affected Files:**
  - `sfa_api/sfa_api/Infrastructure/Persistence/DataSeeder.cs:30–43`

- **Problem:** `DataSeeder.cs` creates the default admin user with the hardcoded password `"Admin@1234"` and then logs `logger.LogInformation("Admin user seeded — username: admin / password: Admin@1234")` on every application startup until an admin exists. This message is written to the console, Serilog file sinks, and the Seq structured logging server.

- **Why it matters:** Default credentials in source code are a known attack vector — automated tools actively scan public repositories for them. Logging the plaintext password to structured log systems means it can appear in log aggregators, error reporting tools, or cloud log services where it may be indexed and retained long-term. An attacker with log read access gains instant admin access to the application.

- **What should be considered:**
  - Source the seed password from an environment variable (e.g., `SFA_ADMIN_SEED_PASSWORD`).
  - The log message must never include the plaintext password — emit only `"Admin user seeded."` with no credentials.
  - Consider running the seeder only in development and staging environments.

---

### #003 — AuditInterceptor Bug: ChangedBy Is Always Guid.Empty

- **Severity:** 🔴 Critical
- **Category:** Architecture / Data Integrity
- **Affected Files:**
  - `sfa_api/sfa_api/Common/Audit/AuditInterceptor.cs:37`

- **Problem:** The interceptor attempts to parse the JWT `sub` claim as a GUID: `Guid.TryParse(userIdClaim, out var userId)`. However, the `sub` claim in the JWT is an **integer** user ID (e.g., `"3"`). `Guid.TryParse("3", ...)` always returns `false`, so `changedBy` is always `Guid.Empty`. Every single row in the `AuditLogs` table will have `ChangedBy = 00000000-0000-0000-0000-000000000000`.

- **Why it matters:** The audit trail is a critical compliance and forensic feature. It is entirely broken — it records no information about who performed any action. All audit queries of the form "who changed this record?" are permanently meaningless. This renders the `AuditLogs` table valueless.

- **What should be considered:**
  - Change `AuditLog.ChangedBy` from `Guid` to `int?` to match the User entity's integer primary key.
  - Replace `Guid.TryParse` with `int.TryParse` in the interceptor.
  - A database migration is required to change the column type.
  - Add a test that verifies `ChangedBy` is populated correctly after a write operation.

---

### #004 — Access Tokens Logged to Console in auth.ts

- **Severity:** 🔴 Critical
- **Category:** Security
- **Affected Files:**
  - `sfa_web/auth.ts:88–91`

- **Problem:** Every successful login triggers `console.log("API Login Response:", JSON.stringify(response.data, null, 2))`, which dumps the full API response — including the raw `accessToken` JWT — to stdout in **all environments including production**.

- **Why it matters:** Access tokens in console output will appear in container stdout logs, cloud log aggregators (CloudWatch, Datadog, GCP Logging), and any log management system that collects process output. Tokens in logs can be extracted and used to impersonate users for the full 5-hour token lifetime. This is a direct OWASP A09 (Security Logging and Monitoring Failures) violation.

- **What should be considered:**
  - Remove the `console.log` call immediately.
  - Sensitive fields (tokens, passwords, secrets) must never be logged.
  - Replace with a redacted debug-mode log: `logger.debug("Login successful for user:", user.id)`.
  - Audit the entire codebase for any other `console.log` calls that may leak sensitive data.

---

### #005 — Missing Database Transaction in PurchaseOrder Create and Update

- **Severity:** 🔴 Critical
- **Category:** Data Integrity
- **Affected Files:**
  - `sfa_api/sfa_api/Features/PurchaseOrders/Services/PurchaseOrderService.cs:120–149` (CreateAsync)
  - `sfa_api/sfa_api/Features/PurchaseOrders/Services/PurchaseOrderService.cs:157–229` (UpdateAsync)

- **Problem:** `CreateAsync` calls `await _repo.SaveChangesAsync(ct)` **twice** — once after creating the `PurchaseOrder` header, and again after adding `PurchaseOrderItems` and `PurchaseOrderHistory`. There is no enclosing database transaction. If the second save fails (e.g., due to an invalid `ProductId` foreign key violation), the `PurchaseOrder` header persists in the database with zero items and zero history — a corrupted, orphaned record.

- **Why it matters:** Data integrity in the purchase order workflow is business-critical. A partial order — existing as a header with no items — cannot be detected or cleaned up by normal application logic. It will appear in lists, can be acted upon, and will produce incorrect financial totals. Under concurrent load or network failure mid-request, this condition can occur silently.

- **What should be considered:**
  - Wrap all operations in `CreateAsync`, `UpdateAsync`, and every state-transition method in a single `await using var tx = await _context.Database.BeginTransactionAsync(ct)` block with `await tx.CommitAsync(ct)` at the end.
  - The first intermediate `SaveChangesAsync` in `CreateAsync` can be eliminated — EF Core's identity columns can be read after a single final `SaveChangesAsync` by configuring the sequence properly, or by restructuring the create flow.
  - Add integration tests that simulate second-save failures to verify rollback behavior.

---

### #006 — SSL Certificate Validation Unconditionally Disabled in auth.ts - ignore

- **Severity:** 🔴 Critical
- **Category:** Security
- **Affected Files:**
  - `sfa_web/auth.ts:9–13`

- **Problem:** The Axios instance in `auth.ts` is created with `rejectUnauthorized: false` hardcoded — no environment check. This is different from `lib/api/client.ts` which correctly gates it on `process.env.NODE_ENV === "production"`. The NextAuth credentials provider will therefore accept any SSL certificate — including self-signed, expired, or forged ones — when calling the API's login endpoint in **all environments including production**.

- **Why it matters:** This enables a Man-in-the-Middle attack on the login endpoint. An attacker who can intercept traffic between the Next.js server and the API server can present any certificate and silently capture every user's username and password on every login. All user credentials pass through this insecure code path.

- **What should be considered:**
  - Change to `rejectUnauthorized: process.env.NODE_ENV === "production"` (matching the pattern in `lib/api/client.ts`).
  - In production, HTTPS must be enforced with a valid, trusted certificate.
  - Audit all other Axios instances in the project for the same misconfiguration.

---

## High Issues

---

### #007 — N+1 Query Problem When Loading PurchaseOrder History Performers

- **Severity:** 🟠 High
- **Category:** Performance
- **Affected Files:**
  - `sfa_api/sfa_api/Features/PurchaseOrders/Services/PurchaseOrderService.cs:38–44`

- **Problem:** After fetching the order history, the service iterates over each unique performer ID and makes a **separate database call per user**:

  ```csharp
  foreach (var pid in performerIds)
  {
      var user = await _userRepo.GetUserByIdAsync(pid, ct);
      performers[pid] = user?.Name;
  }
  ```

  An order with 10 history entries from 5 different users executes 5 sequential database round-trips — in addition to the initial order and history queries.

- **Why it matters:** As purchase orders accumulate history through their workflow (submit → approve → reject → re-submit → finalize), the performer lookup grows linearly. Under concurrent load with many users viewing order details, this multiplies database connections significantly and increases response latency.

- **What should be considered:**
  - Add a batch query method to `IUserRepository`: `Task<Dictionary<int, string>> GetNamesByIdsAsync(IEnumerable<int> ids, CancellationToken ct)` using `.Where(u => ids.Contains(u.Id))`.
  - Replace the foreach loop with a single query that fetches all performer names at once.
  - Alternatively, join performers directly in the history query at the repository level.

---

### #008 — No Access Token Refresh Mechanism in the Web Frontend

- **Severity:** 🟠 High
- **Category:** Authentication / User Experience
- **Affected Files:**
  - `sfa_web/auth.ts` — NextAuth session max age: 24 hours
  - `sfa_api/sfa_api/appsettings.json:16` — API access token expiry: 300 minutes (5 hours)
  - `sfa_web/lib/api/client.ts` — response interceptor handles errors but not token refresh

- **Problem:** The API access token expires after 5 hours. The NextAuth session lasts 24 hours. There is no mechanism to detect a 401 from an expired access token and silently refresh it using the refresh token endpoint. After 5 hours, every API call returns 401 and the user experiences a broken application — empty tables, failed mutations — with no redirect to login and no error message.

- **Why it matters:** This is a silent, user-facing breakage affecting 100% of web users on any session longer than 5 hours. It is especially harmful for roles that keep the app open all day (managers reviewing orders, admins managing users).

- **What should be considered:**
  - Implement token refresh in the NextAuth `jwt` callback: check `token.accessTokenExpiry` and call `POST /api/v1/auth/refresh` before expiry.
  - Alternatively, intercept 401 responses in `lib/api/client.ts`, attempt a refresh, and retry the original request.
  - Store the `accessTokenExpiry` timestamp in the NextAuth JWT and session.
  - Reduce the access token lifetime to 15 minutes and rely on silent refresh for longer sessions.

---

### #009 — Rate Limiting Is Ineffective Behind Reverse Proxies

- **Severity:** 🟠 High
- **Category:** Security / Infrastructure
- **Affected Files:**
  - `sfa_api/sfa_api/Common/Extensions/RateLimitExtensions.cs:20`

- **Problem:** The rate limiter partitions by `ctx.Connection.RemoteIpAddress?.ToString()`. When deployed behind a load balancer, API gateway, or reverse proxy (NGINX, AWS ALB, Cloudflare), `RemoteIpAddress` is the **proxy's IP address** — identical for every client. All clients share one rate limit bucket, making the limiter useless for per-client enforcement and breaking brute-force protection on the `/auth` endpoint.

- **Why it matters:** In production, an attacker can send unlimited password guessing attempts to `POST /api/v1/auth/login`. The configured 10 requests/60 seconds limit on the `auth` policy applies to the proxy's IP, not the attacker's IP. The rate limiter provides a false sense of security while offering no real protection.

- **What should be considered:**
  - Add `UseForwardedHeaders()` middleware (before rate limiting) with `ForwardedHeadersOptions` configured to trust known proxy IPs.
  - Change the partition key to use the resolved client IP from `X-Forwarded-For` or `X-Real-IP` header.
  - Consider user-ID-based rate limiting for authenticated endpoints using the JWT `sub` claim as the partition key.

---

### #010 — Access Token Expiry Is Too Long (5 Hours) - ignore

- **Severity:** 🟠 High
- **Category:** Security
- **Affected Files:**
  - `sfa_api/sfa_api/appsettings.json:16` — `AccessTokenExpiryMinutes: 300`

- **Problem:** JWT access tokens expire after 300 minutes (5 hours). The `RevokedTokens` table only tracks refresh tokens — there is no mechanism to revoke an access token before it expires. If an access token is stolen (via log exposure, XSS, network interception), the attacker has unrestricted API access for up to 5 hours.

- **Why it matters:** Stateless JWTs cannot be invalidated mid-life. A stolen access token is a skeleton key to the entire API for its full lifetime. Industry best practice for sensitive financial/business applications is 15–60 minutes. The longer the window, the greater the blast radius of any token compromise.

- **What should be considered:**
  - Reduce `AccessTokenExpiryMinutes` to 15 minutes.
  - Ensure the web and mobile frontends implement silent refresh via the refresh token.
  - Consider adding access token revocation support using the `jti` claim (already present in the JWT) backed by the existing `RevokedTokens` infrastructure.

---

### #011 — Hardcoded Device ID in auth.ts Bypasses Device Binding - ignore

- **Severity:** 🟠 High
- **Category:** Security
- **Affected Files:**
  - `sfa_web/auth.ts:73` — `deviceId: "test-device-001"`

- **Problem:** The `deviceId` passed to the login endpoint is hardcoded to `"test-device-001"` for every web user. The backend's refresh token system uses `DeviceId` for device binding — meaning all web users share the same device ID. The device-isolation security feature is completely bypassed for the entire web client population.

- **Why it matters:** Device binding is designed to detect and reject refresh token usage from unexpected devices. With all users having the same device ID, a stolen refresh token from one user's web session can be used from any other device without triggering the device mismatch check. The security feature is present but non-functional.

- **What should be considered:**
  - Generate a stable, unique per-browser device ID (a UUID stored in a secure, HTTP-only cookie).
  - Pass this UUID as `deviceId` on every login and refresh call.
  - The device ID must survive page refreshes but can be regenerated on browser data clear (forcing re-login).

---

### #012 — Hard Delete of PurchaseOrderItems Violates Soft-Delete Rule

- **Severity:** 🟠 High
- **Category:** Data Integrity / Architecture
- **Affected Files:**
  - `sfa_api/sfa_api/Features/PurchaseOrders/Repositories/PurchaseOrderRepository.cs:118–123`

- **Problem:** `RemoveItemsAsync` calls `_context.PurchaseOrderItems.RemoveRange(items)` — a hard delete. The project's global rule in `never-do.md` explicitly states: _"Never hard-delete records — soft-delete/deactivate via `IsActive = false`; never call `context.Remove()`."_ This violation destroys line-item history permanently on every order update.

- **Why it matters:** While a JSON snapshot in `PurchaseOrderHistory` partially compensates, the snapshot is stored as unqueryable raw text. There is no way to run product-level queries across order revisions (e.g., "how many units of Product X were ordered across all draft revisions?"), perform forensic analysis, or reconstruct item state for a specific revision without parsing JSON.

- **What should be considered:**
  - Add `IsActive` and `IsDeleted` flags to `PurchaseOrderItem`.
  - On update, mark old items `IsActive = false` / `IsDeleted = true` rather than removing them.
  - New items for the new version are inserted alongside the deactivated old items.
  - This keeps full item revision history queryable at the database level.

---

### #013 — No Upper Bound on the pageSize Query Parameter

- **Severity:** 🟠 High
- **Category:** Performance / Security
- **Affected Files:**
  - All list endpoints: `UsersController.cs`, `DistributorsController.cs`, `RegionsController.cs`, etc.

- **Problem:** All list endpoints accept `[FromQuery] int pageSize = 10` with no maximum value constraint. A request to `GET /api/v1/users?pageSize=100000` will attempt to load, map, and serialize every user record in the database in a single response.

- **Why it matters:** This is both a DoS vector and a memory exhaustion risk. A single malicious or misconfigured request can overwhelm the database, consume all heap memory, and produce a response too large to serialize. Rate limiting does not protect against this because one request is sufficient.

- **What should be considered:**
  - Enforce a maximum page size in each repository or service: `pageSize = Math.Clamp(pageSize, 1, 200)`.
  - Alternatively, add a validation attribute or a shared action filter that rejects requests with `pageSize > 200`.
  - Standardize this maximum across all features.

---

### #014 — User Display Name Set to Email in NextAuth Session

- **Severity:** 🟠 High
- **Category:** Correctness / Security
- **Affected Files:**
  - `sfa_web/auth.ts:111` — `name: user.email`

- **Problem:** When building the NextAuth user object on successful login, `name` is set to `user.email` instead of `user.name`. Every component that renders `session.user.name` — the sidebar user menu, avatar, greeting — will show the user's email address instead of their display name.

- **Why it matters:** This exposes the user's email address in UI contexts where it may not be appropriate (e.g., shown to other users, captured in screen recordings). It also indicates the login response mapping was not tested end-to-end and may suggest other fields are similarly mishandled.

- **What should be considered:**
  - Change line 111 to `name: user.name` using the `name` field from the API's user response object.
  - Add an E2E test that verifies the displayed username after login matches the account's name, not email.

---

## Medium Issues

---

### #015 — No Global Query Filter for IsActive — Manual Filtering Required Everywhere

- **Severity:** 🟡 Medium
- **Category:** Architecture / Data Integrity
- **Affected Files:**
  - `sfa_api/sfa_api/Infrastructure/Persistence/AppDbContext.cs` — explicit comments note the absence of `HasQueryFilter`
  - All repository implementations across all 13 features

- **Problem:** EF Core's `HasQueryFilter` is intentionally absent. Every repository method that should return only active records must manually append `.Where(x => x.IsActive)`. If any developer adds a new query method and forgets this filter, inactive records silently leak to clients with no compile-time or runtime guard.

- **Why it matters:** This is a recurring, latent bug vector. As the codebase grows and new developers add repository methods, a single omission produces subtle, hard-to-diagnose data leaks (inactive users appearing in role assignment dropdowns, deactivated products showing in pricing, etc.). There is no automated enforcement.

- **What should be considered:**
  - Use `HasQueryFilter` for entities where active-only querying is the standard path (Products, Regions, Areas, Territories, etc.).
  - For management views requiring both active and inactive records, add `IgnoreQueryFilters()` explicitly — this makes the exception visible in code rather than the filter being the invisible exception.
  - Add integration tests that verify inactive records are excluded from default list queries.

---

### #016 — String Search May Cause EF Core Client-Side Evaluation on PostgreSQL

- **Severity:** 🟡 Medium
- **Category:** Performance
- **Affected Files:**
  - `sfa_api/sfa_api/Features/PurchaseOrders/Repositories/PurchaseOrderRepository.cs:39–40`
  - Various other repository `GetAllAsync` methods using `.Contains(search, StringComparison.OrdinalIgnoreCase)`

- **Problem:** Search queries use `.ToLower().Contains(...)` or `.Contains(search, StringComparison.OrdinalIgnoreCase)`. EF Core + Npgsql may not fully translate `StringComparison.OrdinalIgnoreCase` to a PostgreSQL-native operator. Depending on the Npgsql version, this may silently fall back to client-side evaluation — loading the entire table into application memory before filtering.

- **Why it matters:** Client-side evaluation is a silent, catastrophic performance regression. In development with small datasets, everything appears to work fine. In production with thousands of records, the first search request can pull the entire `PurchaseOrders` or `Users` table into memory. This only manifests under real data volume.

- **What should be considered:**
  - Use `EF.Functions.ILike(x.Name, $"%{search}%")` for all PostgreSQL case-insensitive search. This translates directly to the native `ILIKE` operator, is properly indexed, and executes entirely in the database.
  - Add index coverage for frequently searched columns (e.g., `OrderNumber`, `Name`) to ensure `ILIKE` uses an index rather than a full table scan.

---

### #017 — No Mechanism to Detect Expired Access Tokens in the Web Client

- **Severity:** 🟡 Medium
- **Category:** User Experience / Reliability
- **Affected Files:**
  - `sfa_web/lib/api/client.ts` — response interceptor
  - `sfa_web/lib/actions/wrapper.ts` — server action wrapper

- **Problem:** The Axios response interceptor catches 401 errors and wraps them in `ApiError`, but there is no logic to distinguish an expired access token from an invalid one, and no attempt to refresh. The `createAction` wrapper propagates the error as `ActionResponse<never>` with `success: false`. The result is that TanStack Query marks queries as failed and UI components show an error state with no recovery path.

- **Why it matters:** Users who keep the application open for more than 5 hours will experience progressive, silent failures — data tables that stop loading, mutations that silently fail — with no redirect to login. This is a critical user experience failure for the target personas (managers and admins who keep the app open all day).

- **What should be considered:**
  - In the Axios response interceptor, detect `status === 401` with `code === "AUTH_TOKEN_EXPIRED"`, call the NextAuth refresh endpoint, update the session, and retry the original request.
  - Alternatively, implement the refresh in the NextAuth `jwt` callback using the `accessTokenExpiry` field stored in the session.

---

### #018 — MemoryCacheService Is Not Safe for Horizontal Scaling

- **Severity:** 🟡 Medium
- **Category:** Performance / Scalability
- **Affected Files:**
  - `sfa_api/sfa_api/Infrastructure/Caching/MemoryCacheService.cs`
  - `sfa_api/sfa_api/Program.cs:77–78`

- **Problem:** `MemoryCacheService` wraps `IMemoryCache`, which is an in-process, single-instance store. When multiple API pods are deployed (Kubernetes, App Service scale-out), each instance has its own isolated cache with no synchronization. Cache writes on one pod are invisible to other pods.

- **Why it matters:** If `ICacheService` is used to cache pricing data, user sessions, or idempotency metadata, different instances will have inconsistent views. Under horizontal scaling this can cause stale data to be served, idempotent requests to be processed twice (if idempotency falls back to the cache), and unpredictable behavior that only manifests in production under load.

- **What should be considered:**
  - Replace `MemoryCacheService` with a Redis-backed implementation using `IDistributedCache` from `Microsoft.Extensions.Caching.StackExchangeRedis`.
  - The existing `ICacheService` interface is already abstract — the implementation swap is isolated.
  - Use Redis also for idempotency key storage if the system scales beyond a single instance.

---

### #019 — Idempotency Keys Are Generated Per-Request, Defeating Their Purpose

- **Severity:** 🟡 Medium
- **Category:** Architecture / Data Integrity
- **Affected Files:**
  - `sfa_web/lib/api/client.ts:97` — `config.headers["X-Idempotency-Key"] = crypto.randomUUID()`

- **Problem:** A new UUID is generated for every outgoing request, regardless of whether it is a fresh operation or a retry. Idempotency is designed to protect against retries of the **same logical operation**. Since each attempt gets a new key, the server's idempotency store never matches a retry — duplicate submissions from double-clicks, network timeouts, or browser retries will always be treated as new requests.

- **Why it matters:** The server-side idempotency infrastructure (`PostgresIdempotencyService`, `IdempotencyMiddleware`) is in place and working, but the client undermines its purpose. Purchase order creation, pricing updates, and any other write operations are therefore vulnerable to double-submission.

- **What should be considered:**
  - The idempotency key must be stable for the duration of one logical user action — generated once when the user initiates the action (e.g., clicks "Submit Order") and reused on any retries within a time window.
  - Store the key in a React ref or mutation state, and clear it only on confirmed success or explicit user reset.
  - Do not generate a new UUID on every request at the interceptor level.

---

### #020 — FluentValidation Boilerplate Duplicated Across Every Controller

- **Severity:** 🟡 Medium
- **Category:** Code Quality / Maintainability
- **Affected Files:**
  - Every controller with a POST or PUT endpoint — 12+ controllers, 20+ action methods

- **Problem:** Every controller action that accepts a request body repeats the same 6-line validation block: call `ValidateAsync`, check `IsValid`, group errors by `PropertyName`, build a dictionary, and throw `ValidationException`. This pattern is duplicated verbatim across all 12 feature controllers.

- **Why it matters:** This violates DRY and creates a maintenance burden. If the validation error format needs to change (e.g., adding field-level error codes, changing the grouping key), it must be updated in 20+ places. It also obscures the controller's business logic with mechanical plumbing.

- **What should be considered:**
  - Extract to an extension method on `IValidator<T>`: `validator.ValidateOrThrow(request, ct)` that performs the validation and throws `ValidationException` directly.
  - Alternatively, use `AddFluentValidationAutoValidation()` from the `FluentValidation.AspNetCore` package to run validators automatically via the model binding pipeline, eliminating all manual calls.

---

### #021 — No Published API Documentation for Integrating Clients

- **Severity:** 🟡 Medium
- **Category:** DevOps / Developer Experience
- **Affected Files:**
  - `sfa_api/sfa_api/Program.cs:201–205` — Swagger gated behind `IsDevelopment()`

- **Problem:** Swagger UI is only available in the development environment and is not published anywhere. There is no exported OpenAPI spec file in the repository and no API documentation portal. The mobile app (when built) and any external integrations have no authoritative reference for endpoint contracts.

- **Why it matters:** Without published API documentation, mobile development requires constant reference to source code. Contract mismatches between the API and clients are harder to catch. Onboarding new developers to either the mobile or web client takes significantly longer.

- **What should be considered:**
  - Add a CI step that exports the OpenAPI JSON: `dotnet swagger tofile --output docs/openapi.json ...`.
  - Commit the generated `openapi.json` to the repository and publish it via Redoc or SwaggerHub.
  - Consider enabling Swagger in staging behind authentication for client developers.

---

### #022 — Health Check Endpoints Are Unauthenticated and Expose Infrastructure State --ignore

- **Severity:** 🟡 Medium
- **Category:** Security
- **Affected Files:**
  - `sfa_api/sfa_api/Common/Extensions/HealthCheckExtensions.cs:20–31`

- **Problem:** `/health/live` and `/health/ready` are mapped with no authentication or IP restriction. The `/health/ready` endpoint actively probes the live PostgreSQL connection and exposes whether the database is reachable. There are no custom health checks beyond PostgreSQL connectivity.

- **Why it matters:** Unauthenticated health endpoints assist attacker reconnaissance — they confirm the system is running and which infrastructure is available. More importantly, the absence of health checks for the token revocation service and idempotency service means a degraded infrastructure state may go undetected by monitoring.

- **What should be considered:**
  - Restrict health check endpoints to known IP ranges (load balancer CIDR, Kubernetes control plane) using middleware or network policy.
  - Add `IHealthCheck` implementations for the token revocation store, idempotency service, and cache service.
  - Consider returning only `Healthy`/`Unhealthy` (no component details) on the public endpoint, with full details only on an authenticated internal endpoint.

---

### #023 — No Request Body Size Limits Configured

- **Severity:** 🟡 Medium
- **Category:** Security / Performance
- **Affected Files:**
  - `sfa_api/sfa_api/Program.cs` — no `UseRequestSizeLimit` or Kestrel body size configuration
  - `sfa_api/sfa_api/Features/PurchaseOrders/Controllers/PurchaseOrdersController.cs:98` — unbounded `Items` list

- **Problem:** No global or per-endpoint request body size limits are configured. The most vulnerable endpoint is `POST /api/v1/purchase-orders`, which accepts an array of order items with no documented or enforced maximum count. A request with thousands of items would consume CPU for validation, memory for deserialization, and database time for inserts.

- **Why it matters:** This is a low-effort DoS vector. A single request with a massive payload can exhaust application memory. Combined with the missing transaction issue (#005), this can leave many orphaned partial records in the database.

- **What should be considered:**
  - Configure Kestrel: `services.Configure<KestrelServerOptions>(o => o.Limits.MaxRequestBodySize = 1_048_576)` (1 MB is reasonable for this domain).
  - Add a FluentValidation rule to `CreatePurchaseOrderValidator`: `RuleFor(x => x.Items).Must(i => i.Count <= 500).WithMessage("Maximum 500 items per order")`.
  - Apply `[RequestSizeLimit]` attribute to endpoints that legitimately need larger payloads.

---

### #024 — DataSeeder Executes a Database Query on Every Application Startup

- **Severity:** 🟡 Medium
- **Category:** Performance / Architecture
- **Affected Files:**
  - `sfa_api/sfa_api/Infrastructure/Persistence/DataSeeder.cs:8`
  - `sfa_api/sfa_api/Program.cs:183`

- **Problem:** `DataSeeder.SeedAsync` is called unconditionally at startup in `Program.cs`. Even though it short-circuits with a check `if (adminExists) return`, it still opens a database connection and executes `SELECT ... WHERE Role = Admin` on every cold start in every environment, including production.

- **Why it matters:** In Kubernetes environments with rolling deploys, liveness-probe restarts, or multi-pod startup, this adds unnecessary database load on every pod restart. The seeder also produces an `AuditLog` record on first boot (via `SaveChangesAsync`) with no authenticated user context, further polluting the audit trail.

- **What should be considered:**
  - Gate the seeder behind an environment check: run only in Development and Staging.
  - Move seed data to EF Core migrations for production (using `migrationBuilder.InsertData` or a separate idempotent migration).
  - The production admin account should be provisioned through a secure, audited process — not an automatic seeder.

---

## Low Issues

---

### #025 — Program.cs Has 100+ Lines of Manual DI Registrations

- **Severity:** 🔵 Low
- **Category:** Code Quality / Maintainability
- **Affected Files:**
  - `sfa_api/sfa_api/Program.cs:104–179`

- **Problem:** Every feature's repository, service, and validators are manually registered in `Program.cs`. With 13 features and 3–5 registrations each, this section is 75+ lines. Every new feature requires editing this central file, creating merge conflict risk when two developers add features simultaneously.

- **Why it matters:** As features grow, this section will become a significant source of friction and merge conflicts. It also obscures the middleware pipeline — the most security-critical part of the file — with registration boilerplate.

- **What should be considered:**
  - Each feature exposes a static extension method (e.g., `AddUsersFeature(this IServiceCollection services)`) that encapsulates its own registrations.
  - `Program.cs` becomes `services.AddUsersFeature().AddDistributorsFeature()...` — concise and conflict-free.
  - Use `FluentValidation`'s `AddValidatorsFromAssembly()` to auto-register all validators by convention.

---

### #026 — No Environment-Specific Configuration Files or Strategy

- **Severity:** 🔵 Low
- **Category:** DevOps / Production Readiness
- **Affected Files:**
  - `sfa_api/sfa_api/` — only `appsettings.json` exists, no `appsettings.Production.json`

- **Problem:** There is no `appsettings.Production.json`, `appsettings.Staging.json`, or documented strategy for how production configuration values are injected. The single `appsettings.json` contains development defaults and hardcoded secrets.

- **Why it matters:** Without environment separation, it is easy to accidentally run production with development settings (overly verbose logging, dev-only endpoints active, wrong CORS origins). A new team member or CI pipeline has no clear guidance on configuring the production system safely.

- **What should be considered:**
  - Create `appsettings.Production.json` with: empty secret placeholders, `Error`-level-only logging, strict CORS (`AllowedOrigins: []`), and the dev-token endpoint disabled.
  - Document that secrets are injected via environment variables using ASP.NET Core's convention: `ConnectionStrings__DefaultConnection`, `Jwt__SecretKey`, etc.
  - Add environment validation at startup to fail fast if required production secrets are missing.

---

### #027 — No CI/CD Pipeline or Automated Deployment Configuration

- **Severity:** 🔵 Low
- **Category:** DevOps / Production Readiness
- **Affected Files:**
  - Repository root — no `.github/workflows/`, no deployment scripts

- **Problem:** No GitHub Actions workflows, no compose files, and no deployment scripts exist. There is a root `Dockerfile` for the API but no pipeline that builds it, runs tests, or deploys it. The 899-test suite provides excellent coverage only if it runs automatically on every merge.

- **Why it matters:** Without CI/CD, deployments are manual and error-prone. Breaking changes can be merged without running tests. There is no audit trail of what was deployed when. Production incidents cannot be rolled back quickly.

- **What should be considered:**
  - Add a GitHub Actions workflow: on every PR, run `dotnet test`, `npm run build`, and `npx playwright test`.
  - Add a deployment pipeline triggered on merge to `main`: build Docker image, push to registry, deploy to staging, run smoke tests, promote to production.
  - Add branch protection rules requiring CI to pass before merge.

---

### #028 — PurchaseOrder Status Enum Stored as Integer — Fragile to Refactoring

- **Severity:** 🔵 Low
- **Category:** Data Integrity / Architecture
- **Affected Files:**
  - `sfa_api/sfa_api/Infrastructure/Persistence/AppDbContext.cs:254` — `HasConversion<int>()`
  - `sfa_api/sfa_api/Features/PurchaseOrders/Enums/PurchaseOrderStatus.cs`

- **Problem:** `PurchaseOrderStatus` is stored as an integer. The current 7-value enum maps to integers 0–6. If values are ever inserted in the middle, reordered, or assigned different explicit integer values during a refactor without a corresponding database migration, every existing order will silently transition to the wrong status.

- **Why it matters:** This is a silent data corruption risk that only surfaces after a deploy. In a financial workflow system, an order silently changing from `Finalized` to `Draft` or `Cancelled` due to an enum reorder would have serious business consequences.

- **What should be considered:**
  - Store enum values as strings using `HasConversion<string>()`. String storage is self-documenting, immune to ordering changes, and human-readable in the database.
  - Alternatively, lock the current integer assignments with explicit `= 0`, `= 1` etc. declarations and a code comment: `// DO NOT REORDER — values are stored in the database`.
  - Add a unit test that asserts specific enum values have not changed.

---

### #029 — No Per-User or Per-Endpoint Rate Limiting on Business-Critical Operations

- **Severity:** 🔵 Low
- **Category:** Security / Performance
- **Affected Files:**
  - `sfa_api/sfa_api/Common/Extensions/RateLimitExtensions.cs`

- **Problem:** Rate limiting is only global (100 req/60s per IP) and auth-specific (10 req/60s). There are no per-user limits or endpoint-specific limits for expensive operations like `POST /api/v1/purchase-orders`, `PUT /api/v1/pricing-structures/{id}/items/bulk`, or the approval workflow endpoints.

- **Why it matters:** A legitimate user (or a compromised account) could trigger expensive operations repeatedly — creating hundreds of purchase orders, running bulk pricing updates, or spamming state transitions. Per-user limits prevent this without impacting other users, and IP-based limits don't help for authenticated users behind NAT.

- **What should be considered:**
  - Add rate limiter policies keyed by the authenticated user's JWT `sub` claim using `PartitionedRateLimiter` with a user-ID partition key.
  - Apply stricter per-user limits on write endpoints: e.g., 10 order creations per minute.
  - Apply even stricter limits on the bulk operations endpoint.

---

### #030 — Flutter Mobile App Is Not Initialized --ignore

- **Severity:** 🔵 Low
- **Category:** Architecture / Project Completeness
- **Affected Files:**
  - `sfa_mobile/` — contains only `CLAUDE.md`, no `pubspec.yaml` or Dart code

- **Problem:** The `sfa_mobile/` directory is a placeholder. No Flutter project has been initialized. The API's core business workflows — outlet visits, daily sales activity, route management — are explicitly designed for field sales reps using the mobile app.

- **Why it matters:** The most critical user persona in the system (field sales reps) has no client. All backend features supporting mobile workflows (Routes, Outlets, Divisions) are built and tested but remain entirely unused. The business value of the system is incomplete until the mobile app is built.

- **What should be considered:**
  - Initialize the Flutter project with the dependencies documented in `sfa_mobile/CLAUDE.md`.
  - Use `flutter_secure_storage` for token storage (already mandated in `never-do.md`).
  - Use `go_router` for navigation, `riverpod` or `flutter_bloc` for state management.
  - Plan for offline-first architecture using `sqflite` or `isar` — field sales reps frequently operate in low-connectivity environments.

---

### #031 — AuditLog Table Will Grow Unbounded With No Retention Policy

- **Severity:** 🔵 Low
- **Category:** Performance / Operations
- **Affected Files:**
  - `sfa_api/sfa_api/Common/Audit/AuditInterceptor.cs` — writes on every `SaveChangesAsync`
  - `sfa_api/sfa_api/Infrastructure/Persistence/AppDbContext.cs:54–59` — `AuditLogs` table

- **Problem:** The `AuditInterceptor` writes a record to `AuditLogs` for every entity change on every `SaveChangesAsync` call. A single purchase order state transition can write 2–3 audit records (order header update + history record + items). There is no cleanup job, archiving strategy, or TTL policy for the `AuditLogs` table.

- **Why it matters:** In production with hundreds of orders per day and active use of activate/deactivate on users and distributors, the `AuditLogs` table can accumulate millions of rows within months. Without a covering index on `ChangedAt`, even filtered queries on this table degrade over time, impacting all writes (since `SaveChangesAsync` reads the change tracker).

- **What should be considered:**
  - Add a background `IHostedService` (similar to the existing `IdempotencyCleanupService`) that purges `AuditLog` entries older than a configurable retention period.
  - Consider table partitioning by `ChangedAt` in PostgreSQL for very high write volumes.
  - Define a retention policy: e.g., operational audit logs retained 90 days, compliance-relevant logs (order finalizations, user creation/deletion) retained 7 years.

---

### #032 — Missing Content Security Policy and Frontend Error Monitoring

- **Severity:** 🔵 Low
- **Category:** Security / Observability
- **Affected Files:**
  - `sfa_web/next.config.ts` — no CSP headers configured
  - `sfa_web/` — no Sentry or equivalent frontend error tracking

- **Problem:** The Next.js application has no Content Security Policy (CSP) HTTP header configured. XSS mitigations rely entirely on React's output escaping with no HTTP-level defense in depth. Additionally, there is no frontend error monitoring — JavaScript exceptions, failed mutations, and rendering errors in production are invisible to the development team.

- **Why it matters:** CSP is the primary browser-level defense against XSS. Without it, a successful XSS injection can exfiltrate the user's JWT from the NextAuth session cookie (if not HTTP-only) or make unauthorized API calls. The absence of frontend error monitoring means production issues are discovered only when users report them, significantly increasing MTTR.

- **What should be considered:**
  - Add CSP headers in `next.config.ts` via `headers()` configuration: restrict `script-src`, `connect-src`, `img-src`, and `frame-ancestors` to known safe origins.
  - Integrate Sentry (or similar) for frontend error capture: `@sentry/nextjs` provides automatic Next.js integration with source map support.
  - Ensure the NextAuth session cookie is set with `HttpOnly`, `Secure`, and `SameSite=Strict` flags.

---

## Summary Table

| #   | Title                                                          | Severity    | Category                 |
| --- | -------------------------------------------------------------- | ----------- | ------------------------ |
| 001 | Hardcoded Production Secrets in Source Control                 | 🔴 Critical | Security                 |
| 002 | Default Admin Credentials Hardcoded and Logged                 | 🔴 Critical | Security                 |
| 003 | AuditInterceptor Bug: ChangedBy Always Guid.Empty              | 🔴 Critical | Data Integrity           |
| 004 | Access Tokens Logged to Console in auth.ts                     | 🔴 Critical | Security                 |
| 005 | Missing Database Transaction in PurchaseOrder Operations       | 🔴 Critical | Data Integrity           |
| 006 | SSL Validation Unconditionally Disabled in auth.ts             | 🔴 Critical | Security                 |
| 007 | N+1 Query on PurchaseOrder History Performers                  | 🟠 High     | Performance              |
| 008 | No Access Token Refresh in Web Frontend                        | 🟠 High     | Authentication           |
| 009 | Rate Limiting Ineffective Behind Reverse Proxies               | 🟠 High     | Security                 |
| 010 | Access Token Expiry Too Long (5 Hours)                         | 🟠 High     | Security                 |
| 011 | Hardcoded Device ID Bypasses Device Binding                    | 🟠 High     | Security                 |
| 012 | Hard Delete of PurchaseOrderItems Violates Soft-Delete Rule    | 🟠 High     | Data Integrity           |
| 013 | No Upper Bound on pageSize Query Parameter                     | 🟠 High     | Security / Performance   |
| 014 | User Display Name Set to Email in NextAuth Session             | 🟠 High     | Correctness              |
| 015 | No Global Query Filter for IsActive                            | 🟡 Medium   | Architecture             |
| 016 | String Search May Cause Client-Side Evaluation on PostgreSQL   | 🟡 Medium   | Performance              |
| 017 | No Detection of Expired Access Tokens in Web Client            | 🟡 Medium   | Reliability              |
| 018 | MemoryCacheService Not Safe for Horizontal Scaling             | 🟡 Medium   | Scalability              |
| 019 | Idempotency Keys Generated Per-Request Defeat Their Purpose    | 🟡 Medium   | Data Integrity           |
| 020 | FluentValidation Boilerplate Duplicated Across All Controllers | 🟡 Medium   | Code Quality             |
| 021 | No Published API Documentation for Integrating Clients         | 🟡 Medium   | Developer Experience     |
| 022 | Health Check Endpoints Unauthenticated                         | 🟡 Medium   | Security                 |
| 023 | No Request Body Size Limits Configured                         | 🟡 Medium   | Security / Performance   |
| 024 | DataSeeder Executes DB Query on Every Startup                  | 🟡 Medium   | Performance              |
| 025 | Program.cs Has 100+ Lines of Manual DI Registrations           | 🔵 Low      | Code Quality             |
| 026 | No Environment-Specific Configuration Strategy                 | 🔵 Low      | DevOps                   |
| 027 | No CI/CD Pipeline or Automated Deployment                      | 🔵 Low      | DevOps                   |
| 028 | PurchaseOrder Status Enum Stored as Integer                    | 🔵 Low      | Data Integrity           |
| 029 | No Per-User Rate Limiting on Business-Critical Operations      | 🔵 Low      | Security                 |
| 030 | Flutter Mobile App Not Initialized                             | 🔵 Low      | Architecture             |
| 031 | AuditLog Table Grows Unbounded Without Retention Policy        | 🔵 Low      | Operations               |
| 032 | Missing Content Security Policy and Frontend Error Monitoring  | 🔵 Low      | Security / Observability |
