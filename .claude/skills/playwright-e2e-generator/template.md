# E2E Test Plan: {feature}

Fill in every section before generating any code.

---

## Feature Analysis

| Item | Source | Value |
|------|--------|-------|
| Route | `components/table/{feature}-table.tsx` → DataTable config | `/{features}` |
| Search placeholder | DataTable `searchPlaceholder` config | `"Search {features}..."` |
| Add button label | `renderToolbarContent` in table | `"Add {Feature}"` |
| Page heading | `{feature}-list-page.tsx` | `"{Feature} Management"` |

---

## Dialogs That Exist

| Dialog | Heading | Submit button | Type |
|--------|---------|---------------|------|
| Create | "Create {Feature}" | "Create {Feature}" | Dialog |
| Edit | "Edit {Feature}" | "Update {Feature}" | Dialog |
| Delete | "Delete {Feature}" | "Delete" | AlertDialog |
| Deactivate | "Deactivate {Feature}" | "Deactivate" | AlertDialog |

---

## Form Fields

| Label (exact) | Input type | Validation message |
|---------------|-----------|-------------------|
| Name | text | "Name is required" |
| ... | | |

---

## Column Headers

| Header | Note |
|--------|------|
| {Feature} | combined name+email column |
| Status | Badge: Active / Inactive |
| (empty) | actions column |

---

## Row Action Menu Items

- Edit
- Delete
- Deactivate / Activate (if applicable)

---

## Spec Files to Generate

```
sfa_web/e2e/
├── pages/{feature}.page.ts
└── features/{feature}/
    ├── {feature}-list.spec.ts
    ├── {feature}-create.spec.ts
    ├── {feature}-update.spec.ts
    ├── {feature}-delete.spec.ts
    └── {feature}-deactivate.spec.ts   (if applicable)
```

---

## Test Data Uniqueness Strategy

```typescript
const uniqueSuffix = Date.now().toString(36)

// Different leading digit per spec file prevents phone collisions
// create: +3, update: +1, deactivate: +2, delete: +4
const testData = {
  name: `E2E {Feature} ${uniqueSuffix}`,
  // ...
}
```
