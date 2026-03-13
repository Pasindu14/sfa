# SFA Playwright E2E Conventions Reference

This document contains the exact patterns, selectors, and structures used in the SFA web app's E2E tests. Read this when you need to verify a specific convention or selector pattern.

---

## Table of Contents

1. [Project Config](#project-config)
2. [Auth Setup](#auth-setup)
3. [File Structure](#file-structure)
4. [Selector Patterns](#selector-patterns)
5. [Test Data Generation](#test-data-generation)
6. [POM Method Patterns](#pom-method-patterns)
7. [Spec File Patterns](#spec-file-patterns)
8. [UI Component Selectors](#ui-component-selectors)

---

## Project Config

From `playwright.config.ts`:
- `testDir: './e2e'`
- `fullyParallel: false` — tests run sequentially
- `workers: 1` — single worker
- `timeout: 30_000` per test
- `expect.timeout: 10_000` for assertions
- `slowMo: 500` — 500ms pause between actions
- `baseURL: 'http://localhost:3000'`
- Auth session saved to `playwright/.auth/admin.json`
- `webServer` auto-starts `npm run dev`

Projects:
1. `setup` — runs `auth.setup.ts`, stores session
2. `chromium` — depends on `setup`, uses stored `storageState`
3. `cleanup` — runs `global.teardown.ts`, deletes auth file

---

## Auth Setup

All tests run as admin. The setup flow:
1. Navigate to `/login`
2. Fill username from `E2E_ADMIN_USERNAME` env var (default: `admin`)
3. Fill password from `E2E_ADMIN_PASSWORD` env var (default: `Admin@1234`)
4. Click Login button
5. Wait for redirect to `/users`
6. Save `storageState` to `playwright/.auth/admin.json`

Tests do NOT need to handle login — the session is injected automatically.

---

## File Structure

```
sfa_web/e2e/
├── auth.setup.ts                          # Login + save session
├── global.teardown.ts                     # Clean up auth file
├── pages/
│   └── {feature}.page.ts                  # One POM class per feature
└── features/
    └── {feature}/
        ├── {feature}-list.spec.ts         # Table, search, filter tests
        ├── {feature}-create.spec.ts       # Create dialog + form tests
        ├── {feature}-update.spec.ts       # Edit dialog + form tests
        ├── {feature}-delete.spec.ts       # Delete confirmation tests
        └── {feature}-deactivate.spec.ts   # Status toggle tests
```

Naming:
- Page objects: `{feature}.page.ts` (singular, e.g., `distributor.page.ts`)
- Spec files: `{feature}-{action}.spec.ts` (singular feature, e.g., `distributor-create.spec.ts`)
- Feature folders: singular (e.g., `features/distributor/`, not `features/distributors/`)

The feature folder name inside `e2e/features/` should match the feature folder name in `sfa_web/features/`. So if the web feature is at `features/user/`, E2E tests go in `e2e/features/user/` — but if the web feature is `features/distributor/`, use `e2e/features/distributor/`. However, the existing users tests use `e2e/features/users/` (plural) — for new features, prefer the singular form to match the feature directory, but don't break existing conventions.

---

## Selector Patterns

### Preferred (ARIA-based)

| Element | Selector |
|---------|----------|
| Button by text | `page.getByRole('button', { name: 'Add User' })` |
| Input by label | `dialog.getByLabel('Name', { exact: true })` |
| Input by placeholder | `page.getByPlaceholder('Search users...')` |
| Column header | `page.getByRole('columnheader', { name: 'Username' })` |
| Menu item | `page.getByRole('menuitem', { name: 'Edit' })` |
| Heading | `page.getByRole('heading', { name: 'Create User' })` |
| Select option | `page.getByRole('option', { name: 'Sales Rep' })` |
| Dialog | `page.locator('[role="dialog"]')` |
| Alert dialog | `page.getByRole('alertdialog')` |
| Button in alert | `page.getByRole('alertdialog').getByRole('button', { name: 'Delete' })` |

### Allowed CSS selectors

| Element | Selector | Why CSS needed |
|---------|----------|----------------|
| Sonner success toast | `[data-sonner-toast][data-type="success"]` | No ARIA role exposed |
| Sonner error toast | `[data-sonner-toast][data-type="error"]` | No ARIA role exposed |
| Radix select content | `[data-radix-select-content]` | Needed to wait for close |
| Table body rows | `table tbody tr` | Counting/iterating rows |
| Row action menu trigger | Row's `button` with name "Open menu" | Standard DataTable pattern |

### Forbidden

- `data-testid` — components don't use them
- Class-based selectors (`.btn-primary`, `.user-row`) — fragile
- XPath — never needed
- ID selectors (`#username`) — not used in this project

---

## Test Data Generation

Every spec file creates unique test data at the module level:

```typescript
const uniqueSuffix = Date.now().toString(36)

// Phone numbers: +{leading digit}{last 9 digits of timestamp}
// Use different leading digits per spec file to avoid collisions:
// create: +3, update: +1, deactivate: +2, delete: +4
const uniquePhone = `+3${Date.now().toString().slice(-9)}`

const testData: FeatureFormData = {
  name: `E2E Test Item ${uniqueSuffix}`,
  email: `e2e_${uniqueSuffix}@test.com`,
  phone: uniquePhone,
  // ... other fields with realistic values
}
```

For number fields (like alias, discount, commission):
- Use simple realistic values: `alias: 100`, `tradeDiscount: 10`, `commission: 5`
- The POM's `fillForm` method should convert numbers to strings when filling inputs

---

## POM Method Patterns

### Navigation
```typescript
async goto() {
  await this.page.goto('/{route}')
  await this.page.waitForLoadState('networkidle')
}
```

### Table row lookup
```typescript
// Identify the unique visible column — username shown as @username, or name, etc.
getRowByIdentifier(value: string): Locator {
  return this.table.locator('tr').filter({ hasText: value })
}
```

### Search with debounce
```typescript
async search(query: string) {
  await this.searchInput.fill(query)
  await this.page.waitForTimeout(500) // Client-side filter debounce
}
```

### Form filling (inside dialog)
```typescript
async fillForm(data: Partial<FormData>) {
  const dialog = this.page.locator('[role="dialog"]')
  // For each field: check if defined, clear, fill
  if (data.name !== undefined) {
    await dialog.getByLabel('Name', { exact: true }).clear()
    await dialog.getByLabel('Name', { exact: true }).fill(data.name)
  }
  // For select fields:
  if (data.role !== undefined) {
    await dialog.getByLabel('Role', { exact: true }).click()
    await this.page.getByRole('option', { name: data.role }).click()
    await this.page.locator('[data-radix-select-content]')
      .waitFor({ state: 'hidden', timeout: 3_000 }).catch(() => {})
  }
  // For number fields:
  if (data.discount !== undefined) {
    await dialog.getByLabel('Trade Discount', { exact: true }).clear()
    await dialog.getByLabel('Trade Discount', { exact: true }).fill(String(data.discount))
  }
}
```

### Alert dialog confirmation
```typescript
async confirmAlertAction(buttonName: string) {
  await this.page.getByRole('alertdialog').getByRole('button', { name: buttonName }).click()
}
```

---

## Spec File Patterns

### List spec (independent tests)
```typescript
test.describe('Feature List', () => {
  let featurePage: FeaturePage

  test.beforeEach(async ({ page }) => {
    featurePage = new FeaturePage(page)
    await featurePage.goto()
  })

  test('should display table with data and headers', async () => { ... })
  test('should have Add button visible', async () => { ... })
  test('should search and filter', async () => { ... })
  test('should filter by {field}', async () => { ... })
  test('should show rows after clearing search', async () => { ... })
})
```

### Create spec (independent tests)
```typescript
test.describe('Create Feature', () => {
  test.beforeEach(...)

  test('should open create dialog when clicking Add', async () => { ... })
  test('should show validation errors on empty submit', async () => { ... })
  test('should create successfully', async () => { ... })
  test('should show error on duplicate', async () => { ... })
})
```

### Update spec (serial — needs setup)
```typescript
test.describe.serial('Update Feature', () => {
  let featurePage: FeaturePage

  test('setup: create item to edit', async ({ page }) => {
    featurePage = new FeaturePage(page)
    await featurePage.goto()
    await featurePage.openCreateDialog()
    await featurePage.fillForm(testData)
    await featurePage.submitCreateForm()
    await featurePage.expectSuccessToast()
    await featurePage.expectDialogClosed()
  })

  test('should open edit dialog with pre-filled data', async ({ page }) => { ... })
  test('should update successfully', async ({ page }) => { ... })
  test('should show validation error on invalid input', async ({ page }) => { ... })
})
```

### Deactivate/Activate spec (serial — needs setup)
```typescript
test.describe.serial('Deactivate & Activate Feature', () => {
  test('setup: create item', async ({ page }) => { ... })
  test('should show deactivate confirmation dialog', async ({ page }) => { ... })
  test('should cancel deactivate and keep active', async ({ page }) => { ... })
  test('should deactivate successfully', async ({ page }) => { ... })
  test('should show activate option for inactive item', async ({ page }) => { ... })
  test('should activate successfully', async ({ page }) => { ... })
})
```

### Delete spec (serial — needs setup)
```typescript
test.describe.serial('Delete Feature', () => {
  test('setup: create item to delete', async ({ page }) => { ... })
  test('should show delete confirmation dialog', async ({ page }) => { ... })
  test('should cancel delete', async ({ page }) => { ... })
  test('should delete successfully', async ({ page }) => { ... })
})
```

---

## UI Component Selectors

### DataTable
- Search input: `getByPlaceholder(searchPlaceholder)` — read from table component's config
- Add button: `getByRole('button', { name: 'Add {Feature}' })` — read from `renderToolbarContent`
- Column headers: `getByRole('columnheader', { name: '...' })` — read from columns definition
- Row action trigger: each row has a `button` named "Open menu"
- Action menu items: `getByRole('menuitem', { name: '...' })` — read from columns action menu

### Dialogs (Radix Dialog)
- Container: `[role="dialog"]`
- Heading: `getByRole('heading', { name: 'Create {Feature}' })` or `'Edit {Feature}'`
- Submit buttons: `getByRole('button', { name: 'Create {Feature}' })` or `'Update {Feature}'`
- Close: clicking outside or X button

### Alert Dialogs (Radix AlertDialog)
- Container: `getByRole('alertdialog')`
- Title: look for heading text like "Delete {Feature}" or "Deactivate {Feature}"
- Confirm button: destructive action name — "Delete", "Deactivate", "Activate"
- Cancel button: always named "Cancel"

### Sonner Toasts
- Success: `[data-sonner-toast][data-type="success"]`
- Error: `[data-sonner-toast][data-type="error"]`
- Timeout: 10_000ms (toasts auto-dismiss)

### Forms (react-hook-form + shadcn)
- All inputs have `<Label>` elements — use `getByLabel(text, { exact: true })`
- Select triggers are also labeled — click label to open, pick `getByRole('option')`
- Validation errors appear as text below fields — use `getByText(errorMessage)`
- Field errors from API appear via `useEffect` + `setError` — same assertion pattern
