import { type Page, type Locator, expect } from '@playwright/test'

/** Test data shape for creating / editing a distributor */
export interface DistributorFormData {
  name: string
  alias: number
  address: string
  phone: string
  email: string
  tradeDiscount: number
  commission: number
  remark?: string
  vatRegNo?: string
  latitude?: number
  longitude?: number
}

/**
 * Page Object Model for the /distributors page.
 *
 * Encapsulates every UI interaction so tests stay readable
 * and selector changes only need fixing in one place.
 */
export class DistributorPage {
  // --- Page-level locators ---
  readonly addButton: Locator
  readonly searchInput: Locator
  readonly table: Locator

  constructor(readonly page: Page) {
    this.addButton = page.getByRole('button', { name: 'Add Distributor' })
    this.searchInput = page.getByPlaceholder('Search distributors...')
    this.table = page.locator('table')
  }

  // ─── Navigation ────────────────────────────────────────

  async goto() {
    await this.page.goto('/distributors')
    await this.page.waitForLoadState('networkidle')
  }

  // ─── Table helpers ─────────────────────────────────────

  /** Get a table row that contains the given distributor name */
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

  /** Assert the status badge text for a given distributor row */
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
    // Search fires on Enter (server-side), not on debounce
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
    await this.page.getByRole('menuitem', { name: 'Deactivate' }).click()
  }

  async clickActivate(name: string) {
    await this.openRowActions(name)
    await this.page.getByRole('menuitem', { name: 'Activate' }).click()
  }

  // ─── Dialog interactions ──────────────────────────────

  /** Open the Create Distributor dialog */
  async openCreateDialog() {
    await this.addButton.click()
    await expect(this.page.getByRole('heading', { name: 'Create Distributor' })).toBeVisible()
  }

  /** Fill the distributor form (works for both create and edit dialogs) */
  async fillDistributorForm(data: Partial<DistributorFormData>) {
    const dialog = this.page.locator('[role="dialog"]')

    if (data.name !== undefined) {
      await dialog.getByLabel('Name', { exact: true }).clear()
      await dialog.getByLabel('Name', { exact: true }).fill(data.name)
    }
    if (data.alias !== undefined) {
      await dialog.getByLabel('Alias (Numbers only)', { exact: true }).clear()
      await dialog.getByLabel('Alias (Numbers only)', { exact: true }).fill(String(data.alias))
    }
    if (data.address !== undefined) {
      await dialog.getByLabel('Address', { exact: true }).clear()
      await dialog.getByLabel('Address', { exact: true }).fill(data.address)
    }
    if (data.phone !== undefined) {
      await dialog.getByLabel('Phone', { exact: true }).clear()
      await dialog.getByLabel('Phone', { exact: true }).fill(data.phone)
    }
    if (data.email !== undefined) {
      await dialog.getByLabel('Email', { exact: true }).clear()
      await dialog.getByLabel('Email', { exact: true }).fill(data.email)
    }
    if (data.tradeDiscount !== undefined) {
      await dialog.getByLabel('Trade Discount (%) *', { exact: true }).clear()
      await dialog.getByLabel('Trade Discount (%) *', { exact: true }).fill(String(data.tradeDiscount))
    }
    if (data.commission !== undefined) {
      await dialog.getByLabel('Commission (%) *', { exact: true }).clear()
      await dialog.getByLabel('Commission (%) *', { exact: true }).fill(String(data.commission))
    }
    if (data.vatRegNo !== undefined) {
      await dialog.getByLabel('VAT Registration Number (Optional)').clear()
      await dialog.getByLabel('VAT Registration Number (Optional)').fill(data.vatRegNo)
    }
    if (data.latitude !== undefined) {
      await dialog.getByLabel('Latitude (Optional)').clear()
      await dialog.getByLabel('Latitude (Optional)').fill(String(data.latitude))
    }
    if (data.longitude !== undefined) {
      await dialog.getByLabel('Longitude (Optional)').clear()
      await dialog.getByLabel('Longitude (Optional)').fill(String(data.longitude))
    }
    if (data.remark !== undefined) {
      await dialog.getByLabel('Remark (Optional)').clear()
      await dialog.getByLabel('Remark (Optional)').fill(data.remark)
    }
  }

  /** Click the submit button inside the create dialog */
  async submitCreateForm() {
    await this.page.getByRole('button', { name: 'Create Distributor' }).click()
  }

  /** Click the submit button inside the edit dialog */
  async submitEditForm() {
    await this.page.getByRole('button', { name: 'Update Distributor' }).click()
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
