import { type Page, type Locator, expect } from '@playwright/test'

export interface ProductCategoryFormData {
  name: string
}

export class ProductCategoryPage {
  readonly addButton: Locator
  readonly searchInput: Locator
  readonly table: Locator

  constructor(readonly page: Page) {
    this.addButton = page.getByRole('button', { name: 'Add Category' })
    this.searchInput = page.getByPlaceholder('Search categories...')
    this.table = page.locator('table')
  }

  // ─── Navigation ────────────────────────────────────────

  async goto() {
    await this.page.goto('/product-categories')
    await this.page.waitForLoadState('networkidle')
  }

  // ─── Table helpers ─────────────────────────────────────

  getRowByName(name: string): Locator {
    return this.table.locator('tr').filter({ hasText: name })
  }

  async expectRowExists(name: string) {
    await expect(this.getRowByName(name)).toBeVisible({ timeout: 10_000 })
  }

  async expectRowNotExists(name: string) {
    await expect(this.getRowByName(name)).toBeHidden({ timeout: 10_000 })
  }

  async expectRowStatus(name: string, status: 'Active' | 'Inactive') {
    const row = this.getRowByName(name)
    await expect(row.getByText(status, { exact: true })).toBeVisible({ timeout: 10_000 })
  }

  async expectTableHasRows() {
    await expect(this.table.locator('tbody tr').first()).toBeVisible({ timeout: 10_000 })
  }

  // ─── Search ───────────────────────────────────────────

  async search(query: string) {
    await this.searchInput.fill(query)
    await this.searchInput.press('Enter')
    await this.page.waitForLoadState('networkidle')
  }

  async clearSearch() {
    await this.searchInput.clear()
    await this.searchInput.press('Enter')
    await this.page.waitForLoadState('networkidle')
  }

  // ─── Row actions ──────────────────────────────────────

  async openRowActions(name: string) {
    const row = this.getRowByName(name)
    await row.getByRole('button', { name: 'Open menu' }).click()
  }

  async clickEdit(name: string) {
    await this.openRowActions(name)
    await this.page.getByRole('menuitem', { name: 'Edit', exact: true }).click()
  }

  async clickDeactivate(name: string) {
    await this.openRowActions(name)
    await this.page.getByRole('menuitem', { name: 'Deactivate', exact: true }).click()
  }

  async clickActivate(name: string) {
    await this.openRowActions(name)
    await this.page.getByRole('menuitem', { name: 'Activate', exact: true }).click()
  }

  // ─── Dialog interactions ──────────────────────────────

  async openCreateDialog() {
    await this.addButton.click()
    await expect(this.page.getByRole('dialog')).toBeVisible()
  }

  async fillForm(data: Partial<ProductCategoryFormData>) {
    const dialog = this.page.locator('[role="dialog"]')
    if (data.name !== undefined) {
      await dialog.getByLabel('Name').clear()
      await dialog.getByLabel('Name').fill(data.name)
    }
  }

  async submitCreateForm() {
    await this.page.getByRole('button', { name: 'Create Category' }).click()
  }

  async submitEditForm() {
    await this.page.getByRole('button', { name: 'Update Category' }).click()
  }

  async confirmAlertAction(action: 'Activate' | 'Deactivate') {
    await this.page.getByRole('alertdialog').getByRole('button', { name: action }).click()
  }

  async cancelAlert() {
    await this.page.getByRole('alertdialog').getByRole('button', { name: 'Cancel' }).click()
  }

  // ─── Assertions ───────────────────────────────────────

  async expectSuccessToast(partialText?: string) {
    const toast = this.page.locator('[data-sonner-toast][data-type="success"]').first()
    await expect(toast).toBeVisible({ timeout: 10_000 })
    if (partialText) await expect(toast).toContainText(partialText)
  }

  async expectFieldError(text: string) {
    await expect(this.page.getByText(text).first()).toBeVisible()
  }

  async expectDialogClosed() {
    await expect(this.page.locator('[role="dialog"]')).not.toBeAttached({ timeout: 10_000 })
  }
}
