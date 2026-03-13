# Example: Category E2E Tests

Minimal feature — name + description, no deactivate action.

---

## pages/category.page.ts

```typescript
import { type Page, type Locator, expect } from '@playwright/test'

export interface CategoryFormData {
  name: string
  description?: string
}

export class CategoryPage {
  readonly addButton: Locator
  readonly searchInput: Locator
  readonly table: Locator

  constructor(readonly page: Page) {
    this.addButton = page.getByRole('button', { name: 'Add Category' })
    this.searchInput = page.getByPlaceholder('Search categories...')
    this.table = page.locator('table')
  }

  async goto() {
    await this.page.goto('/categories')
    await this.page.waitForLoadState('networkidle')
  }

  getRowByName(name: string): Locator {
    return this.table.locator('tr').filter({ hasText: name })
  }

  async expectRowExists(name: string) {
    await expect(this.getRowByName(name)).toBeVisible({ timeout: 10_000 })
  }

  async expectRowNotExists(name: string) {
    await expect(this.getRowByName(name)).not.toBeAttached({ timeout: 10_000 })
  }

  async search(query: string) {
    await this.searchInput.fill(query)
    await this.page.waitForTimeout(500)
  }

  async openRowActions(name: string) {
    await this.getRowByName(name).getByRole('button', { name: 'Open menu' }).click()
  }

  async clickEdit(name: string) {
    await this.openRowActions(name)
    await this.page.getByRole('menuitem', { name: 'Edit' }).click()
  }

  async clickDelete(name: string) {
    await this.openRowActions(name)
    await this.page.getByRole('menuitem', { name: 'Delete' }).click()
  }

  async openCreateDialog() {
    await this.addButton.click()
    await expect(this.page.getByRole('heading', { name: 'Create Category' })).toBeVisible()
  }

  async fillForm(data: Partial<CategoryFormData>) {
    const dialog = this.page.locator('[role="dialog"]')
    if (data.name !== undefined) {
      await dialog.getByLabel('Name', { exact: true }).clear()
      await dialog.getByLabel('Name', { exact: true }).fill(data.name)
    }
    if (data.description !== undefined) {
      await dialog.getByLabel('Description', { exact: true }).clear()
      await dialog.getByLabel('Description', { exact: true }).fill(data.description)
    }
  }

  async submitCreateForm() {
    await this.page.locator('[role="dialog"]').getByRole('button', { name: 'Create Category' }).click()
  }

  async submitEditForm() {
    await this.page.locator('[role="dialog"]').getByRole('button', { name: 'Update Category' }).click()
  }

  async confirmAlertAction(buttonName: string) {
    await this.page.getByRole('alertdialog').getByRole('button', { name: buttonName }).click()
  }

  async cancelAlert() {
    await this.page.getByRole('alertdialog').getByRole('button', { name: 'Cancel' }).click()
  }

  async expectSuccessToast(partialText?: string) {
    const toast = this.page.locator('[data-sonner-toast][data-type="success"]').first()
    await expect(toast).toBeVisible({ timeout: 10_000 })
    if (partialText) await expect(toast).toContainText(partialText)
  }

  async expectFieldError(text: string) {
    await expect(this.page.getByText(text).first()).toBeVisible()
  }

  async expectDialogClosed() {
    await expect(this.page.locator('[role="dialog"]')).not.toBeAttached({ timeout: 15_000 })
  }
}
```

---

## features/category/category-list.spec.ts

```typescript
import { test, expect } from '@playwright/test'
import { CategoryPage } from '../../pages/category.page'

test.describe('Category List', () => {
  let categoryPage: CategoryPage

  test.beforeEach(async ({ page }) => {
    categoryPage = new CategoryPage(page)
    await categoryPage.goto()
  })

  test('should display the page heading', async () => {
    await expect(categoryPage.page.getByRole('heading', { name: 'Category Management' })).toBeVisible()
  })

  test('should display table with column headers', async () => {
    await expect(categoryPage.table).toBeVisible()
    await expect(categoryPage.page.getByRole('columnheader', { name: 'Category' })).toBeVisible()
    await expect(categoryPage.page.getByRole('columnheader', { name: 'Status' })).toBeVisible()
  })

  test('should display the Add Category button', async () => {
    await expect(categoryPage.addButton).toBeVisible()
  })

  test('should show results after clearing search', async () => {
    await categoryPage.search('zzz_no_match_xyz')
    await categoryPage.searchInput.clear()
    await categoryPage.page.waitForTimeout(500)
    const rows = categoryPage.table.locator('tbody tr')
    await expect(rows.first()).toBeVisible({ timeout: 10_000 })
  })
})
```

---

## features/category/category-create.spec.ts

```typescript
import { test, expect } from '@playwright/test'
import { CategoryPage } from '../../pages/category.page'

const uniqueSuffix = Date.now().toString(36)
const testData = { name: `E2E Category ${uniqueSuffix}` }

test.describe('Create Category', () => {
  let categoryPage: CategoryPage

  test.beforeEach(async ({ page }) => {
    categoryPage = new CategoryPage(page)
    await categoryPage.goto()
  })

  test('should open create dialog when clicking Add', async () => {
    await categoryPage.openCreateDialog()
    await expect(categoryPage.page.getByRole('heading', { name: 'Create Category' })).toBeVisible()
  })

  test('should show validation error on empty name', async () => {
    await categoryPage.openCreateDialog()
    await categoryPage.submitCreateForm()
    await categoryPage.expectFieldError('Name is required')
  })

  test('should create a category successfully', async () => {
    await categoryPage.openCreateDialog()
    await categoryPage.fillForm(testData)
    await categoryPage.submitCreateForm()
    await categoryPage.expectSuccessToast('Category created')
    await categoryPage.expectDialogClosed()
    await categoryPage.expectRowExists(testData.name)
  })

  test('should show error on duplicate name', async () => {
    // Create once
    await categoryPage.openCreateDialog()
    await categoryPage.fillForm({ name: `Dup_${uniqueSuffix}` })
    await categoryPage.submitCreateForm()
    await categoryPage.expectDialogClosed()

    // Try again with same name
    await categoryPage.openCreateDialog()
    await categoryPage.fillForm({ name: `Dup_${uniqueSuffix}` })
    await categoryPage.submitCreateForm()
    await expect(categoryPage.page.locator('[role="dialog"]')).toBeVisible()
  })
})
```

---

## features/category/category-delete.spec.ts

```typescript
import { test, expect } from '@playwright/test'
import { CategoryPage } from '../../pages/category.page'

const uniqueSuffix = Date.now().toString(36)
const testData = { name: `E2E Del Category ${uniqueSuffix}` }

test.describe.serial('Delete Category', () => {
  let categoryPage: CategoryPage

  test('setup: create category to delete', async ({ page }) => {
    categoryPage = new CategoryPage(page)
    await categoryPage.goto()
    await categoryPage.openCreateDialog()
    await categoryPage.fillForm(testData)
    await categoryPage.submitCreateForm()
    await categoryPage.expectSuccessToast()
    await categoryPage.expectDialogClosed()
  })

  test('should show delete confirmation dialog', async ({ page }) => {
    categoryPage = new CategoryPage(page)
    await categoryPage.goto()
    await categoryPage.clickDelete(testData.name)
    await expect(page.getByRole('alertdialog')).toBeVisible()
    await expect(page.getByRole('heading', { name: 'Delete Category' })).toBeVisible()
  })

  test('should cancel delete and keep category', async ({ page }) => {
    categoryPage = new CategoryPage(page)
    await categoryPage.goto()
    await categoryPage.clickDelete(testData.name)
    await categoryPage.cancelAlert()
    await categoryPage.expectRowExists(testData.name)
  })

  test('should delete category successfully', async ({ page }) => {
    categoryPage = new CategoryPage(page)
    await categoryPage.goto()
    await categoryPage.clickDelete(testData.name)
    await categoryPage.confirmAlertAction('Delete')
    await categoryPage.expectSuccessToast('Category deleted')
    await categoryPage.expectRowNotExists(testData.name)
  })
})
```
