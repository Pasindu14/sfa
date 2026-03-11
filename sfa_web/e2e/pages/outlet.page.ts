import { type Page, type Locator, expect } from '@playwright/test'

/** Test data shape for creating / editing an outlet */
export interface OutletFormData {
  name: string
  address: string
  tel: string
  nicNo: string
  creditLimit: number
  latitude: number
  longitude: number
  outletType: 'Small' | 'Medium' | 'Large'
  outletCategory: 'Wholesale' | 'SMMT'
  routeId?: string // route name as shown in the select
  email?: string
  contactPerson?: string
  vatNo?: string
  ownerDOB?: string
  remarks?: string
  billingPriceType?: 'DealerPrice' | 'OldPrice' | 'MarketPrice'
  provinceCode?: string // province name as shown in the select
  districtCode?: string // district name as shown in the select
}

/**
 * Page Object Model for the /outlets page.
 *
 * Encapsulates every UI interaction so tests stay readable
 * and selector changes only need fixing in one place.
 */
export class OutletPage {
  // --- Page-level locators ---
  readonly addButton: Locator
  readonly searchInput: Locator
  readonly table: Locator

  constructor(readonly page: Page) {
    this.addButton = page.getByRole('button', { name: 'Add Outlet' })
    this.searchInput = page.getByPlaceholder('Search outlets...')
    this.table = page.locator('table')
  }

  // ─── Navigation ────────────────────────────────────────

  async goto() {
    await this.page.goto('/outlets')
    await this.page.waitForLoadState('networkidle')
  }

  // ─── Table helpers ─────────────────────────────────────

  /** Get a table row that contains the given outlet name */
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

  /** Assert the status badge text for a given outlet row */
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
    await this.page.waitForTimeout(500)
  }

  async clearSearch() {
    await this.searchInput.clear()
    await this.page.waitForTimeout(500)
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

  async clickDelete(name: string) {
    await this.openRowActions(name)
    await this.page.getByRole('menuitem', { name: 'Delete' }).click()
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

  /** Open the Create Outlet dialog */
  async openCreateDialog() {
    await this.addButton.click()
    await expect(this.page.getByRole('heading', { name: 'Create Outlet' })).toBeVisible()
  }

  /** Fill the outlet form (works for both create and edit dialogs) */
  async fillOutletForm(data: Partial<OutletFormData>) {
    const dialog = this.page.locator('[role="dialog"]')

    if (data.name !== undefined) {
      await dialog.getByLabel('Name', { exact: true }).clear()
      await dialog.getByLabel('Name', { exact: true }).fill(data.name)
    }
    if (data.nicNo !== undefined) {
      await dialog.getByLabel('NIC Number', { exact: true }).clear()
      await dialog.getByLabel('NIC Number', { exact: true }).fill(data.nicNo)
    }
    if (data.address !== undefined) {
      await dialog.getByLabel('Address', { exact: true }).clear()
      await dialog.getByLabel('Address', { exact: true }).fill(data.address)
    }
    if (data.tel !== undefined) {
      await dialog.getByLabel('Telephone', { exact: true }).clear()
      await dialog.getByLabel('Telephone', { exact: true }).fill(data.tel)
    }
    if (data.email !== undefined) {
      await dialog.getByLabel('Email (Optional)', { exact: true }).clear()
      await dialog.getByLabel('Email (Optional)', { exact: true }).fill(data.email)
    }
    if (data.contactPerson !== undefined) {
      await dialog.getByLabel('Contact Person (Optional)', { exact: true }).clear()
      await dialog.getByLabel('Contact Person (Optional)', { exact: true }).fill(data.contactPerson)
    }
    if (data.vatNo !== undefined) {
      await dialog.getByLabel('VAT Number (Optional)', { exact: true }).clear()
      await dialog.getByLabel('VAT Number (Optional)', { exact: true }).fill(data.vatNo)
    }
    if (data.creditLimit !== undefined) {
      await dialog.getByLabel('Credit Limit', { exact: true }).clear()
      await dialog.getByLabel('Credit Limit', { exact: true }).fill(String(data.creditLimit))
    }
    if (data.latitude !== undefined) {
      await dialog.getByLabel('Latitude', { exact: true }).clear()
      await dialog.getByLabel('Latitude', { exact: true }).fill(String(data.latitude))
    }
    if (data.longitude !== undefined) {
      await dialog.getByLabel('Longitude', { exact: true }).clear()
      await dialog.getByLabel('Longitude', { exact: true }).fill(String(data.longitude))
    }
    if (data.ownerDOB !== undefined) {
      await dialog.getByLabel('Owner Date of Birth (Optional)', { exact: true }).clear()
      await dialog.getByLabel('Owner Date of Birth (Optional)', { exact: true }).fill(data.ownerDOB)
    }
    if (data.remarks !== undefined) {
      await dialog.getByLabel('Remarks (Optional)', { exact: true }).clear()
      await dialog.getByLabel('Remarks (Optional)', { exact: true }).fill(data.remarks)
    }

    // --- Select fields ---

    if (data.outletType !== undefined) {
      await dialog.getByLabel('Outlet Type', { exact: true }).click()
      await this.page.getByRole('option', { name: data.outletType }).click()
      await this.page
        .locator('[data-radix-select-content]')
        .waitFor({ state: 'hidden', timeout: 3_000 })
        .catch(() => {})
    }
    if (data.outletCategory !== undefined) {
      await dialog.getByLabel('Outlet Category', { exact: true }).click()
      await this.page.getByRole('option', { name: data.outletCategory }).click()
      await this.page
        .locator('[data-radix-select-content]')
        .waitFor({ state: 'hidden', timeout: 3_000 })
        .catch(() => {})
    }
    if (data.billingPriceType !== undefined) {
      await dialog.getByLabel('Billing Price Type (Optional)', { exact: true }).click()
      await this.page.getByRole('option', { name: data.billingPriceType }).click()
      await this.page
        .locator('[data-radix-select-content]')
        .waitFor({ state: 'hidden', timeout: 3_000 })
        .catch(() => {})
    }
    if (data.provinceCode !== undefined) {
      await dialog.getByLabel('Province (Optional)', { exact: true }).click()
      await this.page.getByRole('option', { name: data.provinceCode }).click()
      await this.page
        .locator('[data-radix-select-content]')
        .waitFor({ state: 'hidden', timeout: 3_000 })
        .catch(() => {})
    }
    if (data.districtCode !== undefined) {
      await dialog.getByLabel('District (Optional)', { exact: true }).click()
      await this.page.getByRole('option', { name: data.districtCode }).click()
      await this.page
        .locator('[data-radix-select-content]')
        .waitFor({ state: 'hidden', timeout: 3_000 })
        .catch(() => {})
    }
    if (data.routeId !== undefined) {
      await dialog.getByLabel('Route', { exact: true }).click()
      await this.page.getByRole('option', { name: data.routeId }).click()
      await this.page
        .locator('[data-radix-select-content]')
        .waitFor({ state: 'hidden', timeout: 3_000 })
        .catch(() => {})
    }
  }

  /** Click the submit button inside the create dialog */
  async submitCreateForm() {
    await this.page.getByRole('button', { name: 'Create Outlet' }).click()
  }

  /** Click the submit button inside the edit dialog */
  async submitEditForm() {
    await this.page.getByRole('button', { name: 'Update Outlet' }).click()
  }

  /** Confirm an alert dialog (Activate / Deactivate / Delete) */
  async confirmAlertAction(buttonName: 'Activate' | 'Deactivate' | 'Delete') {
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
