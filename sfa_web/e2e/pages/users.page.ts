import { type Page, type Locator, expect } from '@playwright/test'

/** Test data shape for creating / editing a user */
export interface UserFormData {
  name: string
  username: string
  email: string
  phone: string
  role?: 'Admin' | 'SalesRep' | 'Manager'
  deviceId?: string
  password?: string
}

/**
 * Page Object Model for the /users page.
 *
 * Encapsulates every UI interaction so tests stay readable
 * and selector changes only need fixing in one place.
 */
export class UsersPage {
  // --- Page-level locators ---
  readonly addUserButton: Locator
  readonly searchInput: Locator
  readonly roleFilter: Locator
  readonly table: Locator

  constructor(readonly page: Page) {
    this.addUserButton = page.getByRole('button', { name: 'Add User' })
    this.searchInput = page.getByPlaceholder('Search users...')
    this.roleFilter = page.locator('button').filter({ hasText: /All Roles|Admin|Manager|Sales Rep/ }).first()
    this.table = page.locator('table')
  }

  // ─── Navigation ────────────────────────────────────────

  async goto() {
    await this.page.goto('/users')
    await this.page.waitForLoadState('networkidle')
  }

  // ─── Table helpers ─────────────────────────────────────

  /** Get a table row that contains the given username (displayed as @username) */
  getRowByUsername(username: string): Locator {
    return this.table.locator('tr').filter({ hasText: `@${username}` })
  }

  /** Assert a row with the username exists in the table */
  async expectRowExists(username: string) {
    await expect(this.getRowByUsername(username)).toBeVisible({ timeout: 10_000 })
  }

  /** Assert a row with the username does NOT exist */
  async expectRowNotExists(username: string) {
    await expect(this.getRowByUsername(username)).toBeHidden({ timeout: 10_000 })
  }

  /** Assert the status badge text for a given user row */
  async expectRowStatus(username: string, status: 'Active' | 'Inactive') {
    const row = this.getRowByUsername(username)
    await expect(row.getByText(status, { exact: true })).toBeVisible({ timeout: 10_000 })
  }

  /** Assert the table has at least one data row */
  async expectTableHasRows() {
    // tbody tr (skip header row in thead)
    await expect(this.table.locator('tbody tr').first()).toBeVisible({ timeout: 10_000 })
  }

  // ─── Search & filter ──────────────────────────────────

  async search(query: string) {
    await this.searchInput.fill(query)
    // Debounce — wait for table to update
    await this.page.waitForTimeout(500)
  }

  async clearSearch() {
    await this.searchInput.clear()
    await this.page.waitForTimeout(500)
  }

  async filterByRole(role: 'all' | 'Admin' | 'Manager' | 'SalesRep') {
    // Open the role select
    await this.page.locator('button').filter({ hasText: /All Roles|Admin|Manager|Sales Rep/ }).first().click()
    const label = role === 'all' ? 'All Roles' : role === 'SalesRep' ? 'Sales Rep' : role
    await this.page.getByRole('option', { name: label }).click()
    await this.page.waitForTimeout(500)
  }

  // ─── Row actions (dropdown menu) ──────────────────────

  /** Open the "..." actions dropdown for a row */
  async openRowActions(username: string) {
    const row = this.getRowByUsername(username)
    await row.getByRole('button', { name: 'Open menu' }).click()
  }

  async clickEdit(username: string) {
    await this.openRowActions(username)
    await this.page.getByRole('menuitem', { name: 'Edit' }).click()
  }

  async clickDelete(username: string) {
    await this.openRowActions(username)
    await this.page.getByRole('menuitem', { name: 'Delete' }).click()
  }

  async clickResetPassword(username: string) {
    await this.openRowActions(username)
    await this.page.getByRole('menuitem', { name: 'Reset Password' }).click()
  }

  async clickDeactivate(username: string) {
    await this.openRowActions(username)
    await this.page.getByRole('menuitem', { name: 'Deactivate' }).click()
  }

  async clickActivate(username: string) {
    await this.openRowActions(username)
    await this.page.getByRole('menuitem', { name: 'Activate' }).click()
  }

  // ─── Dialog interactions ──────────────────────────────

  /** Open the Create User dialog */
  async openCreateDialog() {
    await this.addUserButton.click()
    await expect(this.page.getByRole('heading', { name: 'Create User' })).toBeVisible()
  }

  /** Fill the user form (works for both create and edit dialogs) */
  async fillUserForm(data: Partial<UserFormData>) {
    const dialog = this.page.locator('[role="dialog"]')

    if (data.name !== undefined) {
      await dialog.getByLabel('Name', { exact: true }).clear()
      await dialog.getByLabel('Name', { exact: true }).fill(data.name)
    }
    if (data.username !== undefined) {
      await dialog.getByLabel('Username', { exact: true }).clear()
      await dialog.getByLabel('Username', { exact: true }).fill(data.username)
    }
    if (data.email !== undefined) {
      await dialog.getByLabel('Email', { exact: true }).clear()
      await dialog.getByLabel('Email', { exact: true }).fill(data.email)
    }
    if (data.phone !== undefined) {
      await dialog.getByLabel('Phone', { exact: true }).clear()
      await dialog.getByLabel('Phone', { exact: true }).fill(data.phone)
    }
    if (data.deviceId !== undefined) {
      await dialog.getByLabel('Device ID (optional)').clear()
      await dialog.getByLabel('Device ID (optional)').fill(data.deviceId)
    }
    if (data.role !== undefined) {
      await dialog.getByLabel('Role', { exact: true }).click()
      const label = data.role === 'SalesRep' ? 'Sales Rep' : data.role
      await this.page.getByRole('option', { name: label }).click()
      // Wait for the select popover to fully close before continuing
      await this.page.locator('[data-radix-select-content]').waitFor({ state: 'hidden', timeout: 3_000 }).catch(() => {})
    }
    if (data.password !== undefined) {
      await dialog.getByLabel('Password', { exact: true }).clear()
      await dialog.getByLabel('Password', { exact: true }).fill(data.password)
    }
  }

  /** Click the submit button inside the dialog */
  async submitCreateForm() {
    await this.page.getByRole('button', { name: 'Create User' }).click()
  }

  async submitEditForm() {
    await this.page.getByRole('button', { name: 'Update User' }).click()
  }

  /** Confirm a destructive alert dialog (Delete / Deactivate / Activate) */
  async confirmAlertAction(buttonName: 'Delete' | 'Activate' | 'Deactivate') {
    await this.page.getByRole('alertdialog').getByRole('button', { name: buttonName }).click()
  }

  /** Cancel an alert dialog */
  async cancelAlert() {
    await this.page.getByRole('alertdialog').getByRole('button', { name: 'Cancel' }).click()
  }

  // ─── Toast assertions ─────────────────────────────────

  /** Wait for a success toast to appear (Sonner renders toasts in a list) */
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
