import { type Page, type Locator, expect } from '@playwright/test'

/** Test data shape for creating / editing a territory */
export interface TerritoryFormData {
  name: string
  areaName?: string
}

/**
 * Page Object Model for the /territories page.
 *
 * Encapsulates every UI interaction so tests stay readable
 * and selector changes only need fixing in one place.
 */
export class TerritoryPage {
  // --- Page-level locators ---
  readonly addButton: Locator
  readonly searchInput: Locator
  readonly table: Locator

  constructor(readonly page: Page) {
    this.addButton = page.getByRole('button', { name: 'Add Territory' })
    this.searchInput = page.getByPlaceholder('Search territories...')
    this.table = page.locator('table')
  }

  // ─── Navigation ────────────────────────────────────────

  async goto() {
    await this.page.goto('/territories')
    await this.page.waitForLoadState('networkidle')
  }

  // ─── Table helpers ─────────────────────────────────────

  /** Get a table row that contains the given territory name */
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

  /** Assert the status badge text for a given territory row */
  async expectRowStatus(name: string, status: 'Active' | 'Inactive') {
    const row = this.getRowByName(name)
    await expect(row.getByText(status, { exact: true })).toBeVisible({ timeout: 10_000 })
  }

  /** Assert the table has at least one data row */
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
    await this.page.getByRole('menuitem', { name: 'Deactivate', exact: true }).click()
  }

  async clickActivate(name: string) {
    await this.openRowActions(name)
    await this.page.getByRole('menuitem', { name: 'Activate', exact: true }).click()
  }

  // ─── Dialog interactions ──────────────────────────────

  /** Open the Create Territory dialog */
  async openCreateDialog() {
    await this.addButton.click()
    await expect(this.page.getByRole('heading', { name: 'Create Territory' })).toBeVisible()
  }

  /** Fill the territory form fields (works for both create and edit dialogs) */
  async fillTerritoryForm(data: Partial<TerritoryFormData>) {
    const dialog = this.page.locator('[role="dialog"]')

    if (data.name !== undefined) {
      await dialog.getByLabel('Name', { exact: true }).clear()
      await dialog.getByLabel('Name', { exact: true }).fill(data.name)
    }

    if (data.areaName !== undefined) {
      // AreaSelect wraps SelectTrigger in a <div>, so getByLabel resolves to
      // the wrapper — use getByRole('combobox') to target the actual trigger.
      // Also wait for it to be enabled: useActiveAreas() loads async and the
      // trigger is disabled while loading.
      const selectTrigger = dialog.getByRole('combobox').first()
      await expect(selectTrigger).toBeEnabled({ timeout: 10_000 })
      await selectTrigger.click()
      await this.page.locator('[role="listbox"]').waitFor({ state: 'visible', timeout: 5_000 })
      await this.page.getByRole('option', { name: data.areaName }).click()
      await this.page.locator('[role="listbox"]').waitFor({ state: 'hidden', timeout: 3_000 }).catch(() => {})
    }
  }

  /**
   * Select the first available area option in the Area dropdown.
   * Waits for the async useActiveAreas() fetch to complete before clicking.
   */
  async selectFirstArea() {
    const dialog = this.page.locator('[role="dialog"]')
    const selectTrigger = dialog.getByRole('combobox').first()
    // Wait for areas to load — trigger is disabled during the fetch
    await expect(selectTrigger).toBeEnabled({ timeout: 10_000 })
    await selectTrigger.click()
    await this.page.locator('[role="listbox"]').waitFor({ state: 'visible', timeout: 5_000 })
    await this.page.getByRole('option').first().click()
    await this.page.locator('[role="listbox"]').waitFor({ state: 'hidden', timeout: 3_000 }).catch(() => {})
  }

  /** Click the submit button inside the create dialog */
  async submitCreateForm() {
    await this.page.getByRole('button', { name: 'Create Territory' }).click()
  }

  /** Click the submit button inside the edit dialog */
  async submitEditForm() {
    await this.page.getByRole('button', { name: 'Update Territory' }).click()
  }

  /** Confirm an alert dialog (Activate / Deactivate) */
  async confirmAlertAction(buttonName: 'Activate' | 'Deactivate') {
    await this.page.getByRole('alertdialog').getByRole('button', { name: buttonName }).click()
  }

  /** Cancel an alert dialog */
  async cancelAlert() {
    await this.page.getByRole('alertdialog').getByRole('button', { name: 'Cancel' }).click()
  }

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
