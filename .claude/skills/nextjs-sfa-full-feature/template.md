# Feature Generation Plan: {entity}

Fill in every section before generating any code.

---

## Entity Summary

| Field | Value |
|-------|-------|
| Entity name (singular, camelCase) | `{entity}` |
| Entity name (singular, PascalCase) | `{Entity}` |
| API base path | `/api/v1/{entities}` |
| Feature directory | `sfa_web/features/{entity}/` |
| App route | `app/(protected)/{entities}/page.tsx` |

---

## DTO Fields (from API response)

| Field | TS Type | Notes |
|-------|---------|-------|
| id    | number  | |
| name  | string  | |
| ...   |         | |
| isActive | boolean | |
| createdAt | string | ISO date |
| updatedAt | string | ISO date |

---

## Create Schema Fields

| Field | Zod type | Validation | Error message |
|-------|----------|-----------|---------------|
| name  | z.string() | .min(1) | "Name is required" |
| ...   | | | |

---

## Update Schema Differences from Create

- [ ] Same as create
- [ ] Omits: ___

---

## Enums

```typescript
export const {field}Enum = z.enum(['Value1', 'Value2'])
```

---

## Table Columns

| Column | Display name | Type | Notes |
|--------|-------------|------|-------|
| name  | Name | combined with another field? | |
| isActive | Status | Badge | Active = default, Inactive = secondary |
| | Actions | DropdownMenu | Edit, Delete |

---

## Custom Actions (beyond CRUD)

| Action | Endpoint | Menu label | Dialog type |
|--------|----------|-----------|-------------|
| deactivate | DELETE /api/v1/{entities}/{id} | Deactivate | AlertDialog |
| activate | PATCH /api/v1/{entities}/{id}/activate | Activate | AlertDialog |

---

## Files to Generate

```
sfa_web/features/{entity}/
├── schema/{entity}.schema.ts
├── actions/{entity}.actions.ts
├── hooks/{entity}.hooks.ts
├── store/
│   ├── {entity}.dialog-store.ts
│   ├── {entity}.filter-store.ts
│   └── index.ts
└── components/
    ├── forms/{entity}-form.tsx
    ├── columns/{entity}-columns.tsx
    ├── table/{entity}-table.tsx
    ├── dialogs/{entity}-dialogs.tsx
    ├── pages/{entity}-list-page.tsx
    ├── types/{entity}.types.ts
    └── index.ts

sfa_web/app/(protected)/{entities}/page.tsx
```

---

## Checklist

- [ ] All imports use correct paths (no broken imports)
- [ ] Schema defines all fields with proper validation
- [ ] Actions use `createAction` wrapper (no manual try/catch)
- [ ] Hooks include all 8 DataTable parameters
- [ ] `isQueryHook` flag set on DataTable hook
- [ ] Mutation hooks expose `fieldErrors` and `clearFieldErrors`
- [ ] Stores use Zustand v5 syntax with devtools
- [ ] App route page uses `dynamic(..., { ssr: false })`
- [ ] `revalidatePath` called after all mutations
