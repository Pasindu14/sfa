# Route Unlock Requests: rep asks, supervisor or admin approves

## Context

On mobile, the bill outlet picker hides every outlet more than 1 km from the rep. The only way around it today is the **admin-granted proximity exemption** (built 2026-09-16). That exemption covers the rep's whole account, lasts for days, and only an admin can start it.

Reps need a self-service path for everyday cases: "I need to reach every shop on my route today." The flow:

1. The rep taps **Request unlock**.
2. Their **supervisor** gets a push and approves or rejects on mobile.
3. An **Admin** can also approve on web if the supervisor is unavailable.
4. Once approved, the rep sees **and can bill** every outlet on **today's assigned route only**, until midnight Sri Lanka time.
5. Every step is kept as a permanent audit trail: who requested it, who approved, rejected, cancelled or revoked it, when, from where, and which bills used it.

**Useful existing pieces:**
- **The server already controls the phone.** `GET /outlets/by-route/{routeId}` returns `geofenceEnforced` and `geofenceEnforcedFrom`, and the phone already honours them in both client-side checks (the picker and `CreateBillBloc`). An approved unlock just has to make the resolver return "not enforced until midnight" for that rep and route.
- **Admin grants stay untouched.** They are a separate table and are still checked first.

## Decisions (assumed; change at review)

- **The unlock covers seeing *and* billing.** The server bill gate (`BillingService.CreateAsync` step ②) honours the unlock too. Otherwise the rep would see shops they can't bill.
- **It applies only to today's assigned route.** The server looks up the route from `DailyRouteAssignment`; the client never sends a route id. The unlock matches only on `(repId, routeId)`.
- **It ends at the Sri Lanka midnight boundary**, `SriLankaTime.StartOfDayUtc(today+1)`, the same as admin grants. No job is needed: a request still pending at midnight shows as **Expired**, and approving it is refused.
- **Who can approve:**
  - The rep's **current direct supervisor**, through the reporting line and `IsRepUnderSupervisorAsync`, on mobile.
  - Any **Admin**, on web.
  - NSM, RSM and ASM cannot approve.
  - A rep with no supervisor can still request; only admins can approve.
- **Actions:**
  - The rep can **cancel** a pending request.
  - The supervisor or an admin can **revoke** a live unlock, with a reason.
  - Only one pending or approved request per rep per day. Up to 3 requests per day in total (`RouteUnlock:MaxRequestsPerDay`), so the rep can re-ask after a rejection or revoke.
- **Precedence:** a live admin grant is rep-wide and wins. Creating a request while already exempt returns `ROUTE_UNLOCK_ALREADY_EXEMPT`.
- **Bills are stamped** with `RouteUnlockRequestId`, but only when the bill was genuinely out of range. This matches the existing `ProximityOverridden` rule.
- **Audit:** `AuditLog` is purged after 90 days, so it can't be the record. A new append-only **events table** is the permanent trail. The interceptor still writes its usual `AuditLog` rows on top.

---

## 1. API (`sfa_api/sfa_api/Features/RouteUnlockRequests/`)

Copy the layout of `Features/UserProximityExemptions/` (Controllers, DTOs, Entities, Repositories, Requests, Services, Validators, ServiceExtensions). Register it in `Program.cs` next to line 250.

**Entities**
- **`RouteUnlockRequest`**
  - Request: `UserId`, `RouteId`, `DailyRouteAssignmentId`, `BusinessDate` (DateOnly, like `AssignedDate`), `Status` (string enum: `Pending | Approved | Rejected | Cancelled | Revoked`, max 20).
  - Context at request time: `RequestReason` (≤500), `RequestedAt`, `RequestLatitude?`, `RequestLongitude?`, `RequestGpsAccuracyMeters?`, and `SupervisorUserId?` (a snapshot of who it was routed to).
  - Review: `ReviewedByUserId?`, `ReviewedByRole?`, `ReviewedAt?`, `ReviewNote?`.
  - Validity window, set on approve: `ValidFrom?`, `ValidTo?`.
  - Revoke and cancel: `RevokedByUserId?`, `RevokedAt?`, `RevokeReason?`, `CancelledAt?`.
  - Standard `IsActive`/`IsDeleted`, audit columns, and an `xmin` `RowVersion`.
- **`RouteUnlockRequestEvent`** (append-only): `RouteUnlockRequestId` (FK), `Action` (Requested/Approved/Rejected/Cancelled/Revoked), `FromStatus?`, `ToStatus`, `PerformedByUserId`, `PerformedByRole`, `PerformedAt`, `Note?`, `IpAddress?`, `CorrelationId?`.
- **Indexes** in `AppDbContext`:
  - A **unique partial** index on `(UserId, BusinessDate) WHERE Status IN ('Pending','Approved') AND NOT IsDeleted`. This is the real guard against double-submit.
  - A resolver lookup index on `(UserId, RouteId, Status, ValidTo)`.
  - A list index on `(Status, BusinessDate)`.
  - On Billings, a partial index on `RouteUnlockRequestId`.

**Resolver change** (`UserProximityExemptions/Services/ProximityPolicyResolver.cs`, `DTOs/ProximityPolicy.cs`)
- New signature: `ResolveAsync(userId, int? routeId = null, DateTime? atUtc = null, ct)`. Update the named-argument callers and the existing resolver tests.
- Evaluation order:
  1. Config kill-switch.
  2. Admin exemption.
  3. If `routeId` is given, look up the approved unlock where `ValidFrom <= at < ValidTo`, through a new `IRouteUnlockRequestRepository.GetEffectiveAsync`.
  4. Otherwise, enforced.
- Add `Source` (`None | AdminExemption | RouteUnlock`) and `RouteUnlockRequestId` to `ProximityPolicy`. The resolver stays the only reader of both tables.
- **`OutletService.GetByRouteIdAsync`** passes `routeId`. It sets `ExemptionReason = "RouteUnlock"` when the unlock is the source. The route-keyed cache is unchanged, so there is nothing to invalidate.
- **`BillingService.CreateAsync` step ②** passes `routeId: outlet.RouteId`. It stamps `RouteUnlockRequestId` when `proximityOverridden && policy.Source == RouteUnlock`.
- Add a `Billing.RouteUnlockRequestId` column (int?, a plain column like `ProximityExemptionId`).

**Service `RouteUnlockRequestService`**
- **Every transition** follows the PO and Billing pattern:
  1. Take the lock `route-unlock:{id}` (409 when busy).
  2. Check the status guard and the `RowVersion`.
  3. Mutate the row and add an event row in **one `SaveChanges`**. No manual transaction is needed, which avoids the Npgsql retry trap.
  4. Notify **after** the save.
- **`CreateAsync`** (rep):
  1. Look up today's assignment with `GetActiveTodayAssignmentForRepAsync` (it exists but has no callers yet). If there is none, return `ROUTE_UNLOCK_NO_ASSIGNMENT`.
  2. If the resolver already says not enforced for that route, return `ROUTE_UNLOCK_ALREADY_EXEMPT`.
  3. Check the daily cap: `ROUTE_UNLOCK_DAILY_LIMIT`.
  4. Snapshot the supervisor from `UserReportingLineRepository.GetActiveByUserIdAsync`.
  5. Insert the request. A unique-index violation maps to 409 `ROUTE_UNLOCK_ALREADY_OPEN`.
  6. Push `ROUTE_UNLOCK_REQUESTED` to the supervisor.
- **`ApproveAsync`** (Supervisor for own rep, or Admin):
  - It must be `Pending`, `BusinessDate == SriLankaTime.Today` (else `ROUTE_UNLOCK_EXPIRED`), and the rep's assignment for today must still be the same route (else `ROUTE_UNLOCK_ASSIGNMENT_CHANGED`).
  - Sets `ValidFrom = now` and `ValidTo = end of the Sri Lanka day`.
  - Pushes `ROUTE_UNLOCK_APPROVED` to the rep. If an admin acted, it also tells the supervisor.
- **`RejectAsync`** (reason required) and **`RevokeAsync`** (Approved and still live; reason required) follow the same scoping. They push `ROUTE_UNLOCK_REJECTED` or `ROUTE_UNLOCK_REVOKED`.
- **`CancelAsync`**: the rep cancels their own `Pending` request.
- **Scoping:** reuse `SupervisorService.EnsureRepUnderSupervisorAsync` / `SupervisorRepository.IsRepUnderSupervisorAsync`. The supervisor's list is filtered with `UserReportingLineRepository.GetDirectReportsAsync`.
- **Status on read:** DTOs expose `effectiveStatus`, which adds **Expired**: a Pending request from a past day, or an Approved one past `ValidTo`.

**Controller** at `api/v1/route-unlock-requests`. It parses claims the same way `ProximityExemptionsController` does.

| Method | Path | Roles |
|---|---|---|
| POST | `/` `{reason, latitude?, longitude?, gpsAccuracyMeters?}` | SalesRep |
| GET | `/my/today` (latest request for today, or null) | SalesRep |
| POST | `/{id}/cancel` `{rowVersion}` | SalesRep (own) |
| GET | `/` paged; `status`, `from`, `to`, `search` | Admin (all), Supervisor (own reps) |
| GET | `/pending-count` | Admin, Supervisor |
| GET | `/{id}`: request, event timeline, and bills stamped with this unlock (bill no, outlet, distance, amount) | Admin, Supervisor (scoped), SalesRep (own) |
| POST | `/{id}/approve` `{rowVersion, note?}` | Admin, Supervisor |
| POST | `/{id}/reject` / `/{id}/revoke` `{rowVersion, reason}` | Admin, Supervisor |

**Other API pieces**
- **Validators** (FluentValidation): reason 3–500 characters, lat/lng ranges, `RowVersion > 0`.
- **Options:** `RouteUnlockOptions { MaxRequestsPerDay = 3 }`.
- **Exceptions:** add the new codes as `SFAException` subclasses in `Common/Errors/SFAException.cs`, next to `OutletProximityException`.
- **Migration:** `AddRouteUnlockRequests` creates the 2 tables and 1 Billings column. It is additive only. **Apply it to Neon before pushing main** (see the deploy-order memory).

## 2. Mobile (`sfa_mobile/lib/`)

**Rep side: new `features/route_unlock/`**
- Layers: data (remote datasource on Dio, model), domain (entity, repository, use cases `GetTodayUnlockRequest`, `RequestRouteUnlock`, `CancelRouteUnlock`), and presentation (`RouteUnlockCubit`). Register them manually in `core/di/injection.dart`, the same way as `purchase_orders`.
- **Outlet picker** (`features/bills/presentation/widgets/outlet_picker.dart`). This only applies while `proximityEnforced` is true.
  - The empty state (L643-682) and the header next to the distance chip (L337-351) get a **Request unlock** button. It opens a bottom sheet with a reason field and quick-fill chips ("GPS not accurate", "Outlet location wrong", "Need to cover the full route"). The sheet sends the current GPS fix.
  - The status strip shows three states:
    - **Pending**: "Waiting for {supervisor}", with a Cancel button.
    - **Rejected**: "Rejected by {name}: {reason}", with a Request again button.
    - **Approved**: the existing `_exemptionBanner` takes over. Change its copy when `exemptionReason == 'RouteUnlock'` to "Unlocked by {name} for today".
  - The cubit loads `/my/today` when the picker opens. If the status is Approved but the local policy is still enforced, it dispatches `SyncDailyOutletsRequested` on the page's `OutletsBloc`, so the app repairs itself after missing a push.
  - Requesting needs a connection: check `ConnectivityService.hasInternet()` and show a message when offline.
- **No new local state.** The request status is fetched live, and the unlock itself arrives through the existing geofence metadata keys. So `DeviceUserGuard` needs no change.

**Supervisor side**
- A new page at `/supervisor/unlock-requests`, added to the `/supervisor` group in `app_router.dart` (redirect-only group; don't add a builder, because of the black-screen issue).
- A tile with a pending-count badge on `SupervisorHomePage` (tiles at L681-803).
- The page lists **Pending** requests and **Today's decisions**. Each card shows rep, route, requested time, reason, and distance to the route at request time.
- Detail sheet: the timeline, **Approve** with an optional note, **Reject** with a required reason, and **Revoke** when the unlock is live. Reuse the reject-reason UI pattern from `purchase_order_detail_page.dart`.

**Push** (`main.dart` L352-413, `_navigateFromNotification`)
- `ROUTE_UNLOCK_REQUESTED` sends the supervisor to the page.
- `ROUTE_UNLOCK_APPROVED`, `REJECTED` and `REVOKED` trigger an outlet re-sync through the existing background sales-rep sync (`core/background/background_sync_service.dart`) and route the rep to home.
- **Fix the existing gap:** `FcmService.registerToken` is also called on session restore (`auth_bloc.dart` `_onAppStarted`), not only on interactive login. Without this, a supervisor who never logs in again never gets the request push.

## 3. Web (`sfa_web/`), Admin only

- Add a new `features/route-unlock-request/`. Base it on `features/route-cancellation/` (actions, hooks, keys, schema, badge, approve and reject dialogs, dialog store, columns, table, list page). Use `requiredRole: 'Admin'` on every action.
- Add the page `app/(protected)/route-unlock-requests/page.tsx` and a sidebar item **Unlock Requests** under Assignments in `components/app-sidebar.tsx` (L67-82), next to Proximity Exemptions.
- **List page:**
  - An "Awaiting review" KPI card, following the route-cancellation list page.
  - Tabs **Pending | All**, using the pattern in `sales-target-list-page.tsx`. The All tab gets a status filter through `renderCustomFilters`, following `purchase-order-table.tsx` L71-92.
  - The pending query refetches every 60 s, so a request an admin needs to handle appears without a page reload.
  - Row actions: Approve (AlertDialog with an optional note), Reject and Revoke (dialog with a required reason). A `CONCURRENCY_CONFLICT` triggers a refetch through `handleErrorToast`.
- **Detail sheet:**
  - Request facts: rep, route, date, reason, the request GPS position (as a Google Maps link), the routed-to supervisor, and the validity window.
  - The **event timeline**, reusing `HistoryTimeline` from `purchase-order-detail-page.tsx` L225-290.
  - **Bills placed using this unlock.**

## 4. Tests

- **API unit tests** (xUnit, Moq, FluentAssertions):
  - The service's state machine: every allowed and forbidden transition, the expired day, a changed assignment, the daily cap, supervisor scoping (a rep who isn't theirs gets 403), admin override, and that an event is written on each transition.
  - Resolver: route-scoped matching (the same rep on another route stays enforced), admin grant precedence, and the window edges.
  - Validators.
- **API integration tests** on SQLite. Follow `ProximityExemptionsApiTests.cs` and the Billing integration seed pattern.
  - The endpoints end to end.
  - The unique partial index returns 409 on a double create.
  - The outlet sync returns `geofenceEnforced=false` for the approved route only.
  - A bill outside the radius on the approved route passes and is stamped; the same bill on another route gets 422 `OUTLET_OUT_OF_RANGE`.
  - Known limit: the `xmin` 409 path can't be tested on SQLite.
- **Mobile:** unit tests for the cubit (pending, approved leading to a resync, rejected, offline), plus a widget test for the picker's status strip. Use a 390x844 surface for ScreenUtil.
- **Web:** `tsc --noEmit`, lint and build pass.

## 5. End-to-end check

1. Run the API locally and apply the migration to the local database. Log in as a SalesRep who has an assignment today and whose reporting line points to a Supervisor.
2. On the mobile app, as the rep, open Create Bill. The far outlets are hidden. Tap Request unlock; the strip shows Pending.
3. As the Supervisor on a second device or emulator, the push arrives. Approve.
4. The rep's picker shows every outlet on the route with the "Unlocked for today" banner. Bill a far outlet; it succeeds.
5. On web, as Admin, open the Unlock Requests detail. The timeline shows Requested and Approved with names, and the bill appears under "Bills placed using this unlock".
6. Repeat with an admin approving on web, and with reject, cancel and revoke. After a revoke, the rep's next sync or resume brings the filter back.

## Known limitation (unchanged from admin grants)

The server checks the unlock against when it **receives** the bill, not when the bill was captured. A bill captured offline at 23:50 and synced after midnight is refused with `OUTLET_OUT_OF_RANGE`. This matches the current exemption behaviour, and the phone clock is never trusted to relax the check.

## Critical files

- **API, new:** `Features/RouteUnlockRequests/**`
- **API, changed:** `ProximityPolicyResolver.cs`, `ProximityPolicy.cs`, `OutletService.cs` (L134-157), `BillingService.cs` (L96-116, stamp ~L322), `Billing.cs`, `AppDbContext.cs`, `SFAException.cs`, `Program.cs`, `appsettings.json`
- **Mobile:** `features/route_unlock/**` (new), `outlet_picker.dart`, `create_bill_page.dart`, `app_router.dart`, `supervisor_home_page.dart`, `injection.dart`, `main.dart`, `auth_bloc.dart`
- **Web:** `features/route-unlock-request/**` (new), `app/(protected)/route-unlock-requests/page.tsx`, `components/app-sidebar.tsx`

---

## Appendix: API contract (fixed; clients build against this)

Base: `/api/v1/route-unlock-requests`. Standard `ApiResponse<T>` envelope, camelCase JSON, **all enums are strings**.

### DTOs

`RouteUnlockRequestDto`
```jsonc
{
  "id": 12, "userId": 7, "userName": "Kamal Perera", "loginName": "kamal",
  "routeId": 3, "routeName": "Kandy Town A",
  "businessDate": "2026-09-30",
  "status": "Pending",            // Pending | Approved | Rejected | Cancelled | Revoked   (stored)
  "effectiveStatus": "Pending",   // status, plus "Expired" = Pending from a past day OR Approved past validTo
  "isCurrentlyEffective": false,  // Approved and now inside [validFrom, validTo)
  "requestReason": "GPS not accurate",
  "requestedAt": "2026-09-30T03:10:00Z",
  "requestLatitude": 7.29, "requestLongitude": 80.63, "requestGpsAccuracyMeters": 12.5,  // nullable
  "supervisorUserId": 4, "supervisorName": "Nimal S",          // nullable: who it was routed to
  "reviewedByUserId": null, "reviewedByName": null, "reviewedByRole": null, // "Supervisor" | "Admin"
  "reviewedAt": null, "reviewNote": null,                        // approve note or reject reason
  "validFrom": null, "validTo": null,                            // set on approve; validTo = SL midnight (UTC instant)
  "revokedByUserId": null, "revokedByName": null, "revokedAt": null, "revokeReason": null,
  "cancelledAt": null,
  "rowVersion": 123456
}
```

`RouteUnlockRequestEventDto`: `{ id, action: "Requested"|"Approved"|"Rejected"|"Cancelled"|"Revoked", fromStatus?, toStatus, performedByUserId, performedByName?, performedByRole, performedAt, note?, ipAddress? }`

`RouteUnlockBillDto`: `{ billingId, billingNumber, billingDate, outletId, outletName, distanceFromOutletMeters?, totalAmount, createdAt }`

`RouteUnlockRequestDetailDto`: `{ request: RouteUnlockRequestDto, events: RouteUnlockRequestEventDto[] (oldest first), bills: RouteUnlockBillDto[] }`

### Endpoints

| Method | Path | Body | Returns | Roles |
|---|---|---|---|---|
| POST | `/` | `{ reason, latitude?, longitude?, gpsAccuracyMeters? }` | `RouteUnlockRequestDto` | SalesRep |
| GET | `/my/today` | – | `RouteUnlockRequestDto \| null` (latest for today) | SalesRep |
| POST | `/{id}/cancel` | `{ rowVersion }` | `RouteUnlockRequestDto` | SalesRep (own, Pending) |
| GET | `/?page&pageSize&search&status&from&to` | – | paged `RouteUnlockRequestDto[]` + `pagination` | Admin (all), Supervisor (direct reports) |
| GET | `/pending-count` | – | `{ count }` (Pending for today, scoped) | Admin, Supervisor |
| GET | `/{id}` | – | `RouteUnlockRequestDetailDto` | Admin, Supervisor (scoped), SalesRep (own) |
| POST | `/{id}/approve` | `{ rowVersion, note? }` | `RouteUnlockRequestDto` | Admin, Supervisor (scoped) |
| POST | `/{id}/reject` | `{ rowVersion, reason }` | `RouteUnlockRequestDto` | Admin, Supervisor (scoped) |
| POST | `/{id}/revoke` | `{ rowVersion, reason }` | `RouteUnlockRequestDto` | Admin, Supervisor (scoped) |

- `status` filter takes an **effectiveStatus** value (`Pending` = pending AND today; `Approved` = currently live; `Expired`; `Rejected`; `Cancelled`; `Revoked`). `from`/`to` are `yyyy-MM-dd` business dates. Ordered by `requestedAt` desc.
- `reason`: 3–500 chars. `note`: ≤500.

### Error codes
`ROUTE_UNLOCK_NO_ASSIGNMENT` (422) · `ROUTE_UNLOCK_ALREADY_EXEMPT` (422) · `ROUTE_UNLOCK_DAILY_LIMIT` (422) · `ROUTE_UNLOCK_EXPIRED` (422) · `ROUTE_UNLOCK_ASSIGNMENT_CHANGED` (422) · `ROUTE_UNLOCK_ALREADY_OPEN` (409) · `ROUTE_UNLOCK_INVALID_STATE` (409) · `ROUTE_UNLOCK_BUSY` (409) · `CONCURRENCY_CONFLICT` (409) · `FORBIDDEN_ACCESS` (403) · `VALIDATION_FAILED` (400)

### Push / in-app notification `data`
`{ "type": "ROUTE_UNLOCK_REQUESTED" | "ROUTE_UNLOCK_APPROVED" | "ROUTE_UNLOCK_REJECTED" | "ROUTE_UNLOCK_REVOKED" | "ROUTE_UNLOCK_CANCELLED", "requestId": "12" }`
- REQUESTED / CANCELLED → supervisor. APPROVED / REJECTED / REVOKED → rep (and the supervisor when an Admin acted).

### Outlet sync (`GET /api/v1/outlets/by-route/{routeId}`)
Unchanged shape. When the route unlock is what relaxes the policy: `geofenceEnforced: false`, `geofenceEnforcedFrom: validTo`, `exemptionReason: "RouteUnlock"`.
