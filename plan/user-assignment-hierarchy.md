# User Assignment & Hierarchy Plan

## Overview

A positional hierarchy where the geographic assignment table IS the org chart.
No separate `ReportsToId` needed — the geographic tree resolves the full reporting chain automatically.

---

## Geographic ↔ Role Mapping

```
Region     ←→  NSM       (National Sales Manager)
Area       ←→  ASM       (Area Sales Manager)
Territory  ←→  Manager   (Sales Manager)
Division   ←→  SalesRep  (Field Sales Representative)
Route          (operational — daily beat plan, not a separate person)
```

### Reporting Chain (implicit from geography)

```
NSM
 └── ASM       (Area's Region → NSM)
      └── Manager    (Territory's Area → ASM)
           └── SalesRep   (Division's Territory → Manager)
```

---

## UserRole Enum (updated)

```csharp
public enum UserRole
{
    Admin,
    NSM,        // National Sales Manager  — assigned to Region
    ASM,        // Area Sales Manager      — assigned to Area
    Manager,    // Sales Manager           — assigned to Territory
    SalesRep,   // Field rep               — assigned to Division
    Distributor // Distributor portal user — linked to Distributor entity
}
```

---

## UserAssignment Table

```
UserAssignment
├── Id
├── UserId              FK → User
├── DivisionId?         FK → Division    (populated for SalesRep)
├── TerritoryId?        FK → Territory   (populated for Manager)
├── AreaId?             FK → Area        (populated for ASM)
├── RegionId?           FK → Region      (populated for NSM)
├── AssignedAt          DateTime         (default: now)
├── UnassignedAt?       DateTime         (null = currently active)
└── Audit fields        (CreatedAt, UpdatedAt, CreatedBy, UpdatedBy)
```

Only ONE geographic FK is populated per row, determined by the user's role.

### Resolving the Reporting Chain

When you need "who is SalesRep X's manager?":
```
SalesRep's Division.TerritoryId
→ find UserAssignment where TerritoryId = X and IsActive
→ that user IS the Manager
→ Manager's Territory.AreaId → active assignment → ASM
→ ASM's Area.RegionId        → active assignment → NSM
```

### Future Override (if needed)

If org chart ever diverges from geography, add `ReportsToId?` on User as an override:
- `null` = derive manager from geographic hierarchy (default)
- populated = use this explicitly (matrix org exception)

---

## Sales Invoice Snapshot Fields

At import time, look up the distributor's geographic chain and snapshot IDs onto each invoice:

```csharp
// Geographic snapshot (from Distributor)
public int? RegionId     { get; set; }
public int? AreaId       { get; set; }
public int? TerritoryId  { get; set; }
public int? DivisionId   { get; set; }

// Org snapshot (from active UserAssignments at time of import)
public int? NsmId        { get; set; }
public int? AsmId        { get; set; }
public int? ManagerId    { get; set; }
public int? SalesRepId   { get; set; }
```

All nullable — import succeeds even if distributor has no geographic assignment yet.

### Import Lookup Logic

```
Distributor.DivisionId
  → active UserAssignment(DivisionId)   → SalesRepId
  → Division.TerritoryId
      → active UserAssignment(TerritoryId) → ManagerId
      → Territory.AreaId
          → active UserAssignment(AreaId)  → AsmId
          → Area.RegionId
              → active UserAssignment(RegionId) → NsmId
```

---

## UI — List Page

| Column | Description |
|---|---|
| User | Name + Role badge |
| Assigned To | Geographic level label + entity name (e.g. "Division: Kandy 01") |
| Assigned On | Date of assignment |
| Status | Active / Inactive badge |
| Actions | Edit, Unassign |

**Filters:** Search by name · Role dropdown · Region cascade filter · Active Only toggle

---

## UI — Create / Edit Form

### Fields

| Field | Type | Notes |
|---|---|---|
| User | AsyncSelect | Search by name; shows role badge |
| Assigned [Level] | AsyncSelect | Label + options change based on user's role (see table below) |
| Assigned Date | Date picker | Defaults to today |

### Role → Geographic Selector Mapping

| User Role | Field Label | Selector |
|---|---|---|
| NSM | Assigned Region | Region AsyncSelect |
| ASM | Assigned Area | Area AsyncSelect |
| Manager | Assigned Territory | Territory AsyncSelect |
| SalesRep | Assigned Division | Division AsyncSelect |
| Admin / Distributor | — | Not assignable (form disabled) |

### Duplicate Assignment Warning

If the selected user already has an active assignment:
> "This user is currently assigned to [Division X]. Saving will unassign them from there."

---

## UI — Unassign Flow

No hard delete. Unassign sets `UnassignedAt = today` and hides the row from the active list.
History is always preserved for reporting — answers "who was assigned to Territory 3 in Jan 2025?"

---

## Implementation Order

1. Extend `UserRole` enum + migration
2. Create `UserAssignment` entity, repository, service, controller (full CRUD + unassign)
3. Add snapshot fields to `SalesInvoice` entity + migration
4. Wire snapshot lookup into the import service
5. Build frontend feature (list page + assign/unassign dialogs)

---

## Notes

- Only one active assignment per user at a time (enforced at service layer)
- `Admin` and `Distributor` roles are not assignable to geography
- Names are NOT stored in the snapshot — only IDs. Names are resolved at query time by joining dimension tables (standard star schema pattern)
- Routes are operational (daily beat plans), not a separate hierarchy level — no separate person assigned
