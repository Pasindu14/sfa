import { type Page, type Locator, expect } from '@playwright/test'

/** Test data shape for creating / editing a route */
export interface RouteFormData {
  name: string
  pinColor?: string
  description?: string
  divisionName?: string
}

/**
 * Page Object Model for the /routes page.
 *
 * Encapsulates every UI interaction so tests stay readable
 * and selector changes only need fixing in one place.
 */
export class RoutePage {
  // --- Page-level locators ---
  readonly addButton: Locator
  readonly searchInput: Locator
  readonly table: Locator

  constructor(readonly page: Page) {
    this.addButton = page.getByRole('button', { name: 'Add Route' })
    this.searchInput = page.getByPlaceholder('Search routes...')
    this.table = page.locator('table')
  }

  // ─── Navigation ────────────────────────────────────────────────────────────

  async goto() {
    await this.page.goto('/routes')
    // Wait for the page header to confirm the client-side component has mounted
    // (avoids networkidle timeout caused by TanStack Query background refetching)
    await this.page.waitForSelector('h1', { timeout: 30_000 })
  }

  // ─── Table helpers ─────────────────────────────────────────────────────────

  /** Get a table row that contains the given route name */
  getRowByName(name: string): Locator {
    return this.table.locator('tr').filter({ hasText: name })
  }

  /** Assert a row with the given name exists in the table */
  async expectRowExists(name: string) {
    await expect(this.getRowByName(name).first()).toBeVisible({ timeout: 10_000 })
  }

  /** Assert a row with the given name does NOT exist */
  async expectRowNotExists(name: string) {
    await expect(this.getRowByName(name)).toBeHidden({ timeout: 10_000 })
  }

  /** Assert the status badge text for a given route row */
  async expectRowStatus(name: string, status: 'Active' | 'Inactive') {
    const row = this.getRowByName(name)
    await expect(row.getByText(status, { exact: true })).toBeVisible({ timeout: 10_000 })
  }

  /** Assert the table has at least one data row */
  async expectTableHasRows() {
    await expect(this.table.locator('tbody tr').first()).toBeVisible({ timeout: 10_000 })
  }

  // ─── Search ────────────────────────────────────────────────────────────────

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

  // ─── Row actions (dropdown menu) ───────────────────────────────────────────

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

  // ─── Dialog interactions ───────────────────────────────────────────────────

  /** Open the Create Route dialog */
  async openCreateDialog() {
    await this.addButton.click()
    await expect(this.page.getByRole('heading', { name: 'Create Route' })).toBeVisible()
  }

  /** Fill the route form fields (works for both create and edit dialogs) */
  async fillRouteForm(data: Partial<RouteFormData>) {
    const dialog = this.page.locator('[role="dialog"]')

    if (data.name !== undefined) {
      await dialog.getByLabel('Name', { exact: true }).clear()
      await dialog.getByLabel('Name', { exact: true }).fill(data.name)
    }

    if (data.pinColor !== undefined) {
      // The text input for pinColor is labeled "Pin Color"
      // There are two inputs in that field group: color picker + text input
      // We target the text input (type=text) inside the form item
      const pinColorTextInput = dialog.locator('input[placeholder="#3b82f6"]')
      await pinColorTextInput.clear()
      await pinColorTextInput.fill(data.pinColor)
    }

    if (data.description !== undefined) {
      const textarea = dialog.locator('textarea')
      await textarea.clear()
      await textarea.fill(data.description)
    }

    if (data.divisionName !== undefined) {
      // DivisionSelect renders a Radix combobox — wait for it to be enabled
      const selectTrigger = dialog.getByRole('combobox').first()
      await expect(selectTrigger).toBeEnabled({ timeout: 10_000 })
      await selectTrigger.click()
      await this.page.locator('[role="listbox"]').waitFor({ state: 'visible', timeout: 5_000 })
      await this.page.getByRole('option', { name: data.divisionName }).click()
      await this.page.locator('[role="listbox"]').waitFor({ state: 'hidden', timeout: 3_000 }).catch(() => {})
    }
  }

  /**
   * Select the first available division option in the Division dropdown.
   * Waits for the async fetch to complete before clicking.
   */
  async selectFirstDivision() {
    const dialog = this.page.locator('[role="dialog"]')
    const selectTrigger = dialog.getByRole('combobox').first()
    await expect(selectTrigger).toBeEnabled({ timeout: 10_000 })
    await selectTrigger.click()
    await this.page.locator('[role="listbox"]').waitFor({ state: 'visible', timeout: 5_000 })
    await this.page.getByRole('option').first().click()
    await this.page.locator('[role="listbox"]').waitFor({ state: 'hidden', timeout: 3_000 }).catch(() => {})
  }

  /** Click the submit button inside the create dialog */
  async submitCreateForm() {
    await this.page.getByRole('button', { name: 'Create Route' }).click()
  }

  /** Click the submit button inside the edit dialog */
  async submitEditForm() {
    await this.page.getByRole('button', { name: 'Update Route' }).click()
  }

  /** Confirm an alert dialog (Activate / Deactivate) */
  async confirmAlertAction(buttonName: 'Activate' | 'Deactivate') {
    await this.page.getByRole('alertdialog').getByRole('button', { name: buttonName }).click()
  }

  /** Cancel an alert dialog */
  async cancelAlert() {
    await this.page.getByRole('alertdialog').getByRole('button', { name: 'Cancel' }).click()
  }

  // ─── Toast assertions ──────────────────────────────────────────────────────

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

  // ─── Form validation assertions ────────────────────────────────────────────

  async expectFieldError(text: string) {
    await expect(this.page.getByText(text).first()).toBeVisible()
  }

  async expectDialogClosed() {
    await expect(this.page.locator('[role="dialog"]')).not.toBeAttached({ timeout: 15_000 })
  }
}
