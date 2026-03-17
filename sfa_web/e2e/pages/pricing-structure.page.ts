import { type Page, type Locator, expect } from '@playwright/test'

/** Test data shape for creating / editing a pricing structure */
export interface PricingStructureFormData {
  name: string
  description?: string
  isDefault?: boolean
}

/**
 * Page Object Model for the /pricing-structures page.
 *
 * Encapsulates every UI interaction so tests stay readable
 * and selector changes only need fixing in one place.
 */
export class PricingStructurePage {
  // --- Page-level locators ---
  readonly addButton: Locator
  readonly searchInput: Locator
  readonly table: Locator

  constructor(readonly page: Page) {
    this.addButton = page.getByRole('button', { name: 'Add Pricing Structure' })
    this.searchInput = page.getByPlaceholder('Search pricing structures...')
    this.table = page.locator('table')
  }

  // ─── Navigation ────────────────────────────────────────

  async goto() {
    await this.page.goto('/pricing-structures')
    await this.page.waitForLoadState('networkidle')
  }

  // ─── Table helpers ─────────────────────────────────────

  /** Get a table row that contains the given pricing structure name */
  getRowByName(name: string): Locator {
    return this.table.locator('tr').filter({ hasText: name })
  }

  /** Assert a row with the given name exists in the table */
  async expectRowExists(name: string) {
    await expect(this.getRowByName(name)).toBeVisible({ timeout: 10_000 })
  }

  /** Assert a row with the given name does NOT exist */
  async expectRowNotExists(name: string) {
    await expect(this.getRowByName(name)).toBeHidden({ timeout: 10_000 })
  }

  /** Assert the status badge text for a given row */
  async expectRowStatus(name: string, status: 'Active' | 'Inactive') {
    const row = this.getRowByName(name)
    await expect(row.getByText(status, { exact: true })).toBeVisible({ timeout: 10_000 })
  }

  /** Assert the table has at least one data row */
  async expectTableHasRows() {
    await expect(this.table.locator('tbody tr').first()).toBeVisible({ timeout: 10_000 })
  }

  // ─── Search & filter ──────────────────────────────────

  async search(query: string) {
    await this.searchInput.fill(query)
    // Debounce — wait for table to update
    await this.page.waitForTimeout(800)
  }

  async clearSearch() {
    await this.searchInput.clear()
    await this.page.waitForTimeout(800)
  }

  // ─── Row actions (dropdown menu) ──────────────────────

  /** Open the "..." actions dropdown for a row */
  async openRowActions(name: string) {
    const row = this.getRowByName(name)
    await row.getByRole('button', { name: 'Open menu' }).click()
  }

  async clickEdit(name: string) {
    await this.openRowActions(name)
    await this.page.getByRole('menuitem', { name: 'Edit' }).click()
  }

  async clickDeactivate(name: string) {
    await this.openRowActions(name)
    await this.page.getByRole('menuitem', { name: 'Deactivate' }).click()
  }

  async clickActivate(name: string) {
    await this.openRowActions(name)
    await this.page.getByRole('menuitem', { name: 'Activate' }).click()
  }

  async clickManageItems(name: string) {
    await this.openRowActions(name)
    await this.page.getByRole('menuitem', { name: 'Manage Items' }).click()
  }

  // ─── Dialog interactions ──────────────────────────────

  /** Open the Create Pricing Structure dialog */
  async openCreateDialog() {
    await this.addButton.click()
    await expect(this.page.getByRole('heading', { name: 'Create Pricing Structure' })).toBeVisible()
  }

  /** Fill the pricing structure form (works for both create and edit dialogs) */
  async fillForm(data: Partial<PricingStructureFormData>) {
    const dialog = this.page.locator('[role="dialog"]')

    if (data.name !== undefined) {
      await dialog.getByLabel('Name', { exact: true }).clear()
      await dialog.getByLabel('Name', { exact: true }).fill(data.name)
    }
    if (data.description !== undefined) {
      await dialog.getByLabel('Description', { exact: true }).clear()
      await dialog.getByLabel('Description', { exact: true }).fill(data.description)
    }
    if (data.isDefault !== undefined) {
      const checkbox = dialog.getByRole('checkbox')
      const isChecked = await checkbox.isChecked()
      if (data.isDefault !== isChecked) {
        await checkbox.click()
      }
    }
  }

  /** Click the submit button inside the create dialog */
  async submitCreateForm() {
    await this.page.getByRole('button', { name: 'Create Pricing Structure' }).click()
  }

  /** Click the submit button inside the edit dialog */
  async submitEditForm() {
    await this.page.getByRole('button', { name: 'Update Pricing Structure' }).click()
  }

  /** Confirm the deactivate alert dialog */
  async confirmDeactivate() {
    await this.page.getByRole('alertdialog').getByRole('button', { name: 'Deactivate' }).click()
  }

  /** Confirm the activate alert dialog */
  async confirmActivate() {
    await this.page.getByRole('alertdialog').getByRole('button', { name: 'Activate' }).click()
  }

  /** Cancel an alert dialog */
  async cancelAlert() {
    await this.page.getByRole('alertdialog').getByRole('button', { name: 'Cancel' }).click()
  }

  // ─── Toast assertions ─────────────────────────────────

  /** Wait for a success toast to appear */
  async expectSuccessToast(partialText?: string) {
    const toast = this.page.locator('[data-sonner-toast][data-type="success"]').first()
    await expect(toast).toBeVisible({ timeout: 10_000 })
    if (partialText) {
      await expect(toast).toContainText(partialText)
    }
  }

  /** Wait for an error toast */
  async expectErrorToast(partialText?: string) {
    const toast = this.page.locator('[data-sonner-toast][data-type="error"]').first()
    await expect(toast).toBeVisible({ timeout: 10_000 })
    if (partialText) {
      await expect(toast).toContainText(partialText)
    }
  }

  // ─── Form validation assertions ───────────────────────

  /** Assert a field-level validation message is visible */
  async expectFieldError(text: string) {
    await expect(this.page.getByText(text).first()).toBeVisible()
  }

  /** Assert the dialog is closed (waits for Radix exit animation + DOM removal) */
  async expectDialogClosed() {
    await expect(this.page.locator('[role="dialog"]')).not.toBeAttached({ timeout: 15_000 })
  }
}
