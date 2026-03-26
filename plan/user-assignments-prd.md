# User Assignments — PRD

## Overview

Manage the sales org chart and geographic territory assignments for field users (NSM, RSM, ASM, Supervisor, SalesRep).

Two concerns are intentionally kept in separate tables:

| Table | Purpose |
|---|---|
| `UserReportingLines` | Who reports to whom (flexible org chart — skip levels allowed) |
| `UserGeoAssignments` | Which geographic territory/division the user covers |

The `Users` table is **not modified**. Both tables are independently managed but displayed together in the UI.

---

## Hierarchy

```
Admin / Head of Sales
    └── NSM  (National Sales Manager)   — covers a Region
          └── RSM  (Regional Sales Manager)  — covers an Area
                └── ASM  (Area Sales Manager)  — covers a Territory
                      └── Supervisor            — covers a Division
                            └── SalesRep        — covers a Division
```

**Skip levels are allowed.** A SalesRep can report directly to an NSM. The reporting line is always set explicitly by the admin — it is never auto-derived from geography.

---

## Database Schema

### Table 1 — `UserReportingLines`

```
UserReportingLines
├── Id                int          PK, identity
├── UserId            int          FK → Users.Id   (the subordinate)
├── ReportsToUserId   int          FK → Users.Id   (the direct manager)
├── EffectiveFrom     date         when this reporting line became active
├── IsActive          bool         only one active line per user at a time
├── CreatedAt         datetime
├── UpdatedAt         datetime
├── CreatedBy         int?         FK → Users.Id
└── UpdatedBy         int?         FK → Users.Id
```

**Indexes:**
```sql
IX_UserReportingLines_UserId                          -- who does user X report to?
IX_UserReportingLines_ReportsToUserId                 -- who reports to manager X?
IX_UserReportingLines_UserId_IsActive                 -- active line for a specific user
```

**Business rules:**
- Only one `IsActive = true` row per `UserId`
- Creating a new reporting line automatically deactivates the previous one (service layer)
- `ReportsToUserId` can reference any active, non-deleted user regardless of role
- `Admin` and `Distributor` roles are not assignable as subordinates

---

### Table 2 — `UserGeoAssignments`

```
UserGeoAssignments
├── Id                int          PK, identity
├── UserId            int          FK → Users.Id
├── DivisionId        int?         FK → Divisions.Id    (nullable — NSM/RSM may not map to a Division)
├── TerritoryId       int?         FK → Territories.Id  (denormalized from Division at write time)
├── AreaId            int?         FK → Areas.Id        (denormalized from Division at write time)
├── RegionId          int?         FK → Regions.Id      (denormalized from Division at write time)
├── EffectiveFrom     date         when this geo assignment started
├── IsActive          bool         only one active assignment per user at a time
├── CreatedAt         datetime
├── UpdatedAt         datetime
├── CreatedBy         int?         FK → Users.Id
└── UpdatedBy         int?         FK → Users.Id
```

**Indexes:**
```sql
IX_UserGeoAssignments_UserId                          -- geo for a specific user
IX_UserGeoAssignments_RegionId                        -- filter table by region
IX_UserGeoAssignments_TerritoryId                     -- filter table by territory
IX_UserGeoAssignments_DivisionId                      -- filter by division
IX_UserGeoAssignments_UserId_IsActive                 -- active geo for a specific user
IX_UserGeoAssignments_IsActive                        -- all active assignments
```

**Business rules:**
- Only one `IsActive = true` row per `UserId`
- Creating a new geo assignment automatically deactivates the previous one
- `TerritoryId`, `AreaId`, `RegionId` are denormalized from the selected `Division` at write time — no JOIN needed for geographic filter queries
- `DivisionId` is nullable; NSM/RSM assigned to broad areas may not have a specific Division
- Deactivating (soft delete) sets `IsActive = false` — no hard deletes ever

---

## API Endpoints

```
GET    /api/v1/user-assignments              paginated list (joins both tables for display)
GET    /api/v1/user-assignments/stats        4 stat card numbers (single DB call)
GET    /api/v1/user-assignments/{id}         single record by UserGeoAssignment.Id
POST   /api/v1/user-assignments              create — writes to both tables atomically
PUT    /api/v1/user-assignments/{id}         update — updates both tables atomically
DELETE /api/v1/user-assignments/{id}         soft delete — sets IsActive = false on both rows

GET    /api/v1/user-assignments/{userId}/subordinates?depth=1    direct reports only
GET    /api/v1/user-assignments/{userId}/subordinates            full subtree (recursive CTE)
```

All endpoints: `[Authorize(Roles = "Admin")]`

---

## Request / Response

### `POST /api/v1/user-assignments` — Create

```json
{
  "userId": 12,
  "reportsToUserId": 5,
  "divisionId": 8,
  "effectiveFrom": "2026-03-26"
}
```

Service writes atomically:
1. Deactivates existing `UserReportingLine` for `userId` (if any)
2. Inserts new `UserReportingLine` (userId, reportsToUserId, effectiveFrom)
3. Deactivates existing `UserGeoAssignment` for `userId` (if any)
4. Looks up `Division` → copies `TerritoryId`, `AreaId`, `RegionId`
5. Inserts new `UserGeoAssignment` (userId, divisionId, + denormalized IDs, effectiveFrom)

### `GET /api/v1/user-assignments` — List Response

```json
{
  "userAssignments": [
    {
      "id": 1,
      "userId": 12,
      "userName": "Dilshan Jayasinghe",
      "userRole": "SalesRep",
      "reportsToUserId": 5,
      "reportsToUserName": "Kamal Perera",
      "divisionId": 8,
      "divisionName": "Colombo 03 Division",
      "territoryId": 3,
      "territoryName": "Western Territory",
      "areaId": 2,
      "areaName": "Colombo Area",
      "regionId": 1,
      "regionName": "Western Region",
      "effectiveFrom": "2026-03-26",
      "isActive": true,
      "createdAt": "...",
      "updatedAt": "..."
    }
  ],
  "totalCount": 24,
  "page": 1,
  "pageSize": 10
}
```

### `GET /api/v1/user-assignments/stats` — Stats Response

```json
{
  "totalAssignments": 24,
  "activeAssignments": 18,
  "activeTerritories": 8,
  "assignmentsThisMonth": 6
}
```

---

## Table View Query (single JOIN, no recursion)

```sql
SELECT
    geo.Id,
    u.Id              AS UserId,
    u.Name            AS UserName,
    u.Role            AS UserRole,
    mgr.Id            AS ReportsToUserId,
    mgr.Name          AS ReportsToUserName,
    geo.DivisionId,
    d.Name            AS DivisionName,
    geo.TerritoryId,
    t.Name            AS TerritoryName,
    geo.AreaId,
    a.Name            AS AreaName,
    geo.RegionId,
    r.Name            AS RegionName,
    geo.EffectiveFrom,
    geo.IsActive
FROM UserGeoAssignments geo
JOIN  Users u    ON geo.UserId = u.Id
LEFT JOIN UserReportingLines rl  ON rl.UserId = u.Id AND rl.IsActive = true
LEFT JOIN Users mgr  ON mgr.Id = rl.ReportsToUserId
LEFT JOIN Divisions   d  ON geo.DivisionId  = d.Id
LEFT JOIN Territories t  ON geo.TerritoryId = t.Id
LEFT JOIN Areas       a  ON geo.AreaId      = a.Id
LEFT JOIN Regions     r  ON geo.RegionId    = r.Id
WHERE geo.IsActive = true          -- or all, depending on filter
ORDER BY u.Name
OFFSET @skip ROWS FETCH NEXT @take ROWS ONLY
```

---

## Subtree Query (recursive CTE — used for drill-down only)

```sql
WITH RECURSIVE subordinates AS (
    -- anchor: the target user
    SELECT rl.UserId, rl.ReportsToUserId, 0 AS depth
    FROM UserReportingLines rl
    WHERE rl.ReportsToUserId = @managerId AND rl.IsActive = true

    UNION ALL

    -- recurse: reports of reports
    SELECT rl.UserId, rl.ReportsToUserId, s.depth + 1
    FROM UserReportingLines rl
    INNER JOIN subordinates s ON rl.ReportsToUserId = s.UserId
    WHERE rl.IsActive = true
)
SELECT * FROM subordinates
-- WHERE depth = 0   → direct reports only (?depth=1)
-- no WHERE depth    → full subtree
```

---

## UI — List Page

**Wireframe:** `ui-wireframes/user_assignments_page.png`

### Stats Cards (top)
| Card | Value |
|---|---|
| Total Assignments | `stats.totalAssignments` |
| Active Assignments | `stats.activeAssignments` |
| Active Territories | `stats.activeTerritories` |
| This Month | `stats.assignmentsThisMonth` |

### Table Columns
| Column | Source |
|---|---|
| User | avatar (initials) + `userName` + `userRole` badge |
| Reports To | `reportsToUserName` |
| Location | `divisionName` + `territoryName` (stacked) |
| Region | `regionName` |
| Assigned Date | `effectiveFrom` |
| Status | `isActive` → Active / Inactive badge |
| Actions | Edit · Delete |

### Filters
| Filter | Param |
|---|---|
| Search | `?search=` (searches `userName`) |
| Role | `?role=` (NSM / RSM / ASM / Supervisor / SalesRep) |
| Region | `?regionId=` |
| Status | `?isActive=true/false` |

---

## UI — Assign / Edit Dialog

**Wireframe:** `ui-wireframes/assign_dialog_v2.png`

```
┌─ Assign User ─────────────────────────────────────┐
│                                                     │
│  USER *                                             │
│  [ Select user... ▼ ]                              │
│  ┌──────────────────────────────────────────────┐  │
│  │  DJ  Dilshan Jayasinghe                       │  │
│  │      SalesRep                                 │  │
│  └──────────────────────────────────────────────┘  │
│  (preview card appears after selection)             │
│                                                     │
│  REPORTS TO *                                       │
│  [ Select manager... ▼ ]                           │
│  (filtered to active, non-Distributor users)        │
│                                                     │
│  ASSIGNED DIVISION                                  │
│  [ Select division... ▼ ]                          │
│  (nullable — top-level roles may not need one)      │
│                                                     │
│  EFFECTIVE FROM *                                   │
│  [ 03/26/2026  📅 ]                                │
│                                                     │
│  [ Cancel ]              [ Save Assignment ]        │
└─────────────────────────────────────────────────────┘
```

**Duplicate assignment warning** (shown inline if user already has active assignment):
> "This user is currently assigned to Colombo 03 Division. Saving will replace that assignment."

---

## Frontend Feature Structure

```
features/user-assignment/
├── schema/
│   └── user-assignment.schema.ts      (Zod schemas + DTOs)
├── actions/
│   └── user-assignment.actions.ts     (server actions — CRUD + stats + subordinates)
├── hooks/
│   └── user-assignment.hooks.ts       (TanStack Query hooks)
├── store/
│   ├── user-assignment.dialog-store.ts
│   ├── user-assignment.filter-store.ts
│   └── index.ts
└── components/
    ├── forms/
    │   └── user-assignment-form.tsx   (User select with preview card + ReportsTo + Division + Date)
    ├── selects/
    │   └── assignable-user-select.tsx (active users, excludes Admin/Distributor)
    ├── columns/
    │   └── user-assignment-columns.tsx
    ├── table/
    │   └── user-assignment-table.tsx  (stats cards + DataTable)
    ├── dialogs/
    │   └── user-assignment-dialogs.tsx
    ├── pages/
    │   └── user-assignment-list-page.tsx
    ├── types/
    │   └── user-assignment.types.ts
    └── index.ts

app/(protected)/user-assignments/
└── page.tsx                           (dynamic import, ssr: false)
```

---

## Performance Notes

- **Table view**: single JOIN query — no recursion, no N+1
- **Geographic filters**: hit indexed denormalized columns (`RegionId`, `TerritoryId`) on `UserGeoAssignments` — no JOIN needed for WHERE clause
- **Stats**: one HTTP request returns all 4 numbers (4 COUNT queries server-side, all indexed)
- **Subtree**: recursive CTE only runs on the drill-down endpoint — never on the list page
- **Dropdowns**: user/division lists cached by TanStack Query (`staleTime: 5 min`) — repeated dialog opens don't re-fetch

---

## Implementation Order

1. `UserReportingLines` entity + migration
2. `UserGeoAssignments` entity + migration
3. Repository — `IUserAssignmentRepository` + impl (list, stats, getById, create, update, delete, subtree)
4. Service — `IUserAssignmentService` + impl (atomic create/update, deactivate-old logic)
5. Controller — all endpoints + `[Authorize(Roles = "Admin")]`
6. `UserAssignmentsServiceExtensions` + register in `Program.cs`
7. Frontend — schema → actions → hooks → stores → form → columns → table → dialogs → list page → route
8. Sidebar entry under Masters

---

## Notes

- No hard deletes ever — `IsActive = false` is the only removal mechanism
- History is preserved: all past rows remain in both tables with `IsActive = false`
- `Admin` and `Distributor` roles are excluded from the assignable user dropdown
- `DivisionId` is nullable in `UserGeoAssignments` — NSM/RSM assigned to broader scopes may omit it
- `ReportsToUserId` has no role-level constraint enforced at DB level — admin is trusted to set sensible reporting lines
- Both tables use `OnDelete: Restrict` on all FKs — no cascades
