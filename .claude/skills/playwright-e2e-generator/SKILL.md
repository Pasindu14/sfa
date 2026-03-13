---
name: playwright-e2e-generator
description: >
  Generate comprehensive Playwright E2E tests for any feature in the SFA web app.
  Use this skill whenever the user asks to create, generate, write, or add E2E tests,
  Playwright tests, or end-to-end tests for a feature like users, distributors, customers,
  leads, orders, products, visits, or tasks. Also trigger when the user says things like
  "test the X feature", "add tests for X", "write e2e for X", "generate playwright specs
  for X", or "I need E2E coverage for X". Even if the user just says "test distributors"
  or "e2e for orders" without explicitly mentioning Playwright, this skill applies because
  Playwright is the only E2E framework in this project.
---

# Playwright E2E Test Generator for SFA Web

Generate production-ready Playwright E2E tests by analyzing the actual feature implementation — schemas, actions, hooks, components, dialogs, columns, and the corresponding API endpoints — then producing a Page Object Model class and spec files that follow the project's established conventions exactly.

## Quick Reference

- **Test directory:** `sfa_web/e2e/`
- **Page objects:** `sfa_web/e2e/pages/{feature}.page.ts`
- **Spec files:** `sfa_web/e2e/features/{feature}/`
- **Config:** `sfa_web/playwright.config.ts`
- **Auth setup:** `sfa_web/e2e/auth.setup.ts` (logs in as admin, saves session)
- **Run tests:** `npm run test:e2e` from `sfa_web/`

For the full conventions and patterns reference, read `references/conventions.md`.

---

## Step 1: Identify and Analyze the Feature

When the user names a feature (e.g., "distributors", "orders", "products"):

1. **Locate the feature directory** at `sfa_web/features/{feature}/`
2. **Read these files in parallel** (they form the complete picture):

| File | What you learn |
|------|---------------|
| `schema/{feature}.schema.ts` | All form fields, validation rules, field types, required vs optional |
| `actions/{feature}.actions.ts` | Every API endpoint the feature calls, request/response shapes |
| `hooks/{feature}.hooks.ts` | Query keys, mutations, what triggers invalidation, toast messages |
| `store/{feature}.dialog-store.ts` | Which dialogs exist (create, edit, delete, activate, deactivate, etc.) |
| `store/{feature}.filter-store.ts` | Search, filter, sort, and pagination capabilities |
| `components/forms/{feature}-form.tsx` | Form field labels, conditional fields per mode (create vs edit) |
| `components/dialogs/{feature}-dialogs.tsx` | Dialog headings, button labels, alert dialog patterns |
| `components/columns/{feature}-columns.tsx` | Table column headers, action menu items |
| `components/table/{feature}-table.tsx` | DataTable config: search placeholder, custom filters, toolbar buttons |
| `components/pages/{feature}-list-page.tsx` | Page heading text |

3. **Check the API controller** at `sfa_api/sfa_api/Features/{Feature}/Controllers/{Feature}Controller.cs` to understand:
   - Endpoint paths and HTTP methods
   - Request validation rules enforced server-side
   - Business rules that produce specific error codes (e.g., duplicate detection)

4. **Check for existing tests** at `sfa_web/e2e/features/{feature}/` and `sfa_web/e2e/pages/{feature}.page.ts` — if tests already exist, inform the user and ask whether to overwrite, extend, or skip.

---

## Step 2: Determine Test Scenarios

Based on the analysis, identify which test groups apply. Every CRUD feature should get these spec files:

| Spec file | Covers | When to include |
|-----------|--------|-----------------|
| `{feature}-list.spec.ts` | Table rendering, column headers, search, filters, empty state | Always |
| `{feature}-create.spec.ts` | Open dialog, validation errors, successful creation, duplicate/conflict errors | If createAction exists |
| `{feature}-update.spec.ts` | Open edit dialog with pre-filled data, conditional fields, successful update, validation | If updateAction exists |
| `{feature}-delete.spec.ts` | Delete confirmation dialog, cancel, successful deletion | If deleteAction exists |
| `{feature}-deactivate.spec.ts` | Deactivate/activate flow, status badge changes, menu item toggling | If activate/deactivate actions exist |

Additional spec files if the feature supports them:
- `{feature}-change-password.spec.ts` — if a password change action exists
- `{feature}-detail.spec.ts` — if there's a detail/view page (not just list)
- `{feature}-export.spec.ts` — if export is enabled in DataTable config

---

## Step 3: Generate the Page Object Model

Create `sfa_web/e2e/pages/{feature}.page.ts` following this exact structure. The POM encapsulates all UI interactions so spec files stay clean and selector changes are isolated.

### POM Structure

```typescript
import { type Page, type Locator, expect } from '@playwright/test'

/** Test data shape for creating / editing a {feature} */
export interface {Feature}FormData {
  // Mirror the create schema fields
  // Mark optional fields with ?
}

export class {Feature}Page {
  // --- Page-level locators ---
  readonly addButton: Locator
  readonly searchInput: Locator
  readonly table: Locator
  // Add filter locators based on filter store

  constructor(readonly page: Page) {
    // Initialize locators using ARIA roles and labels — never CSS selectors
    // Use getByRole, getByPlaceholder, getByLabel, getByText
    // The "Add" button text comes from the toolbar in {feature}-table.tsx
    // The search placeholder comes from DataTable searchPlaceholder config
  }

  // ─── Navigation ────────────────────────────────────────
  async goto() {
    await this.page.goto('/{feature-route}')
    await this.page.waitForLoadState('networkidle')
  }

  // ─── Table helpers ─────────────────────────────────────
  // getRowByIdentifier — use the column that uniquely identifies rows
  // expectRowExists / expectRowNotExists
  // expectRowStatus (if activate/deactivate exists)
  // expectTableHasRows

  // ─── Search & filter ──────────────────────────────────
  // search(query) — fill + waitForTimeout(500)
  // clearSearch()
  // filterBy{Field}(value) — for each custom filter in the DataTable

  // ─── Row actions (dropdown menu) ──────────────────────
  // openRowActions(identifier) — click "Open menu" button in the row
  // click{Action}(identifier) — for each menu item in columns

  // ─── Dialog interactions ──────────────────────────────
  // openCreateDialog()
  // fill{Feature}Form(data: Partial<FormData>)
  //   - Target inputs inside [role="dialog"]
  //   - Use getByLabel with { exact: true } for each field
  //   - Handle select/combobox fields: click label, pick option, wait for popover close
  //   - Handle number inputs: clear then fill with string value
  // submitCreateForm() / submitEditForm()
  // confirmAlertAction(buttonName) — for AlertDialog confirmations
  // cancelAlert()

  // ─── Toast assertions ─────────────────────────────────
  async expectSuccessToast(partialText?: string) {
    const toast = this.page.locator('[data-sonner-toast][data-type="success"]').first()
    await expect(toast).toBeVisible({ timeout: 10_000 })
    if (partialText) await expect(toast).toContainText(partialText)
  }

  async expectErrorToast(partialText?: string) {
    const toast = this.page.locator('[data-sonner-toast][data-type="error"]').first()
    await expect(toast).toBeVisible({ timeout: 10_000 })
    if (partialText) await expect(toast).toContainText(partialText)
  }

  // ─── Form validation assertions ───────────────────────
  async expectFieldError(text: string) {
    await expect(this.page.getByText(text).first()).toBeVisible()
  }

  async expectDialogClosed() {
    await expect(this.page.locator('[role="dialog"]')).not.toBeAttached({ timeout: 15_000 })
  }
}
```

### POM Rules

- **Selectors:** Always prefer ARIA-based selectors (`getByRole`, `getByLabel`, `getByPlaceholder`, `getByText`). Only use CSS selectors for Sonner toasts (`[data-sonner-toast]`) and Radix popovers (`[data-radix-select-content]`).
- **Form fields:** Read the actual `<Label>` text from the form component. Use `{ exact: true }` to avoid ambiguity (e.g., "Name" vs "Username").
- **Select/combobox fields:** After clicking the select trigger, pick the option with `getByRole('option', { name })`, then wait for the Radix popover to close: `await this.page.locator('[data-radix-select-content]').waitFor({ state: 'hidden', timeout: 3_000 }).catch(() => {})`.
- **Number inputs:** Playwright fills inputs as strings, so convert numbers to strings in `fillForm`.
- **Dialog scoping:** Always scope form interactions to `this.page.locator('[role="dialog"]')` to avoid matching background elements.
- **Timeouts:** Use `10_000` for assertions, `15_000` for dialog close, `500` debounce for search/filter.

---

## Step 4: Generate Spec Files

### Spec File Conventions

```typescript
import { test, expect } from '@playwright/test'
import { {Feature}Page, type {Feature}FormData } from '../../pages/{feature}.page'

// Unique test data — prevents collisions across parallel runs
const uniqueSuffix = Date.now().toString(36)
const testData: {Feature}FormData = {
  // Generate unique values for all required fields
  // Use uniqueSuffix in name, identifier, email
  // Use Date.now() slice for phone numbers: `+3${Date.now().toString().slice(-9)}`
  // Use realistic defaults for enum/select fields
}

test.describe('{Group Name}', () => {
  let featurePage: {Feature}Page

  test.beforeEach(async ({ page }) => {
    featurePage = new {Feature}Page(page)
    await featurePage.goto()
  })

  test('should ...', async () => {
    // Test body — call POM methods, never raw selectors
  })
})
```

### Key Patterns

**Independent vs serial tests:**
- Use `test.describe('...')` for tests that don't depend on each other (list, search, filter)
- Use `test.describe.serial('...')` when tests have dependencies (e.g., create → edit → delete flow)
- Serial groups need a `test('setup: ...')` step that creates the test data

**Test data uniqueness:**
- Every spec file generates its own `uniqueSuffix` and test data at the module level
- This ensures tests don't collide when run in sequence or across retries
- Phone numbers: `+{digit}${Date.now().toString().slice(-9)}` — use different leading digits per spec file

**Validation tests:**
- Submit empty form → check dialog stays open
- Clear required field in edit mode → check dialog stays open
- Derive expected validation messages from Zod schema `.min()`, `.email()`, `.regex()` messages

**Duplicate/conflict tests:**
- Create an entity, then try creating another with the same unique field
- The dialog should stay open (check with `await expect(dialog).toBeVisible()`)

**Status toggle tests (activate/deactivate):**
- Use serial describe with setup
- Test the full cycle: Active → Deactivate → confirm Inactive badge → Activate → confirm Active badge
- Verify menu item toggles between "Activate" and "Deactivate"

**Search and filter tests:**
- Search: grab first row's identifier, search for it, verify it's still visible
- Filter: apply filter, verify all visible rows match, then reset
- Empty state: search for nonsense string, clear, verify table repopulates

---

## Step 5: Validate and Present

After generating all files:

1. **List all generated files** with their paths
2. **Summarize test coverage** — how many spec files, how many test cases, what user flows are covered
3. **Note any gaps** — flows that exist in the feature but weren't tested (explain why)
4. **Remind the user** to run `npm run test:e2e` from `sfa_web/` to execute

---

## What NOT to Do

- Never mock API responses — these are real E2E tests against the running app + API
- Never use `data-testid` unless the component already has one — prefer ARIA selectors
- Never hardcode IDs or assume specific database state — always create test data in setup steps
- Never use `page.waitForTimeout()` except for debounce waits (search/filter) — prefer `waitForLoadState` or assertion timeouts
- Never import from feature code (schemas, types, etc.) — the POM and specs are self-contained
- Never write tests for features that don't exist yet — only test implemented features
- Never use `test.describe.serial` for independent tests — only when tests genuinely depend on prior state
- Never skip the Page Object Model — every spec file must go through the POM, never raw selectors in test bodies
