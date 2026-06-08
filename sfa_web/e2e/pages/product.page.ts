import { type Page, type Locator, expect } from '@playwright/test'

/** Test data shape for creating / editing a product */
export interface ProductFormData {
  code: string
  piecesPerPack: number
  itemDescription: string
  printDescription?: string
  imageUrl?: string
  remarks?: string
}

/**
 * Page Object Model for the /products page.
 *
 * Encapsulates every UI interaction so tests stay readable
 * and selector changes only need fixing in one place.
 */
export class ProductPage {
  // --- Page-level locators ---
  readonly addButton: Locator
  readonly searchInput: Locator
  readonly table: Locator

  constructor(readonly page: Page) {
    this.addButton = page.getByRole('button', { name: 'Add Product' })
    this.searchInput = page.getByPlaceholder('Search by code or description...')
    this.table = page.locator('table')
  }

  // ─── Navigation ────────────────────────────────────────

  async goto() {
    await this.page.goto('/products')
    await this.page.waitForLoadState('networkidle')
  }

  // ─── Table helpers ─────────────────────────────────────

  /** Get a table row that contains the given product code */
  getRowByCode(code: string): Locator {
    return this.table.locator('tr').filter({ hasText: code })
  }

  /** Assert a row with the given code exists in the table */
  async expectRowExists(code: string) {
    await expect(this.getRowByCode(code).first()).toBeVisible({ timeout: 10_000 })
  }

  /** Assert a row with the given code does NOT exist */
  async expectRowNotExists(code: string) {
    await expect(this.getRowByCode(code)).toBeHidden({ timeout: 10_000 })
  }

  /** Assert the status badge text for a given product row */
  async expectRowStatus(code: string, status: 'Active' | 'Inactive') {
    const row = this.getRowByCode(code)
    await expect(row.getByText(status, { exact: true })).toBeVisible({ timeout: 10_000 })
  }

  /** Assert the table has at least one data row */
  async expectTableHasRows() {
    await expect(this.table.locator('tbody tr').first()).toBeVisible({ timeout: 10_000 })
  }

  // ─── Search & filter ──────────────────────────────────

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
  async openRowActions(code: string) {
    const row = this.getRowByCode(code)
    await row.getByRole('button', { name: 'Open menu' }).click()
  }

  async clickEdit(code: string) {
    await this.openRowActions(code)
    await this.page.getByRole('menuitem', { name: 'Edit' }).click()
  }

  async clickDeactivate(code: string) {
    await this.openRowActions(code)
    await this.page.getByRole('menuitem', { name: 'Deactivate', exact: true }).click()
  }

  async clickActivate(code: string) {
    await this.openRowActions(code)
    await this.page.getByRole('menuitem', { name: 'Activate', exact: true }).click()
  }

  // ─── Dialog interactions ──────────────────────────────

  /** Open the Create Product dialog */
  async openCreateDialog() {
    await this.addButton.click()
    await expect(this.page.getByRole('heading', { name: 'Create Product' })).toBeVisible()
  }

  /** Fill the product form (works for both create and edit dialogs) */
  async fillProductForm(data: Partial<ProductFormData>) {
    const dialog = this.page.locator('[role="dialog"]')

    if (data.code !== undefined) {
      await dialog.getByLabel('Code *', { exact: true }).clear()
      await dialog.getByLabel('Code *', { exact: true }).fill(data.code)
    }
    if (data.piecesPerPack !== undefined) {
      await dialog.getByLabel('Pieces Per Pack *', { exact: true }).clear()
      await dialog.getByLabel('Pieces Per Pack *', { exact: true }).fill(String(data.piecesPerPack))
    }
    if (data.itemDescription !== undefined) {
      await dialog.getByLabel('Item Description *', { exact: true }).clear()
      await dialog.getByLabel('Item Description *', { exact: true }).fill(data.itemDescription)
    }
    if (data.printDescription !== undefined) {
      await dialog.getByLabel('Print Description (Optional)', { exact: true }).clear()
      await dialog.getByLabel('Print Description (Optional)', { exact: true }).fill(data.printDescription)
    }
    if (data.imageUrl !== undefined) {
      await dialog.getByLabel('Image URL (Optional)', { exact: true }).clear()
      await dialog.getByLabel('Image URL (Optional)', { exact: true }).fill(data.imageUrl)
    }
    if (data.remarks !== undefined) {
      await dialog.getByLabel('Remarks (Optional)', { exact: true }).clear()
      await dialog.getByLabel('Remarks (Optional)', { exact: true }).fill(data.remarks)
    }
  }

  /** Click the submit button inside the create dialog */
  async submitCreateForm() {
    await this.page.getByRole('button', { name: 'Create Product' }).click()
  }

  /** Click the submit button inside the edit dialog */
  async submitEditForm() {
    await this.page.getByRole('button', { name: 'Update Product' }).click()
  }

  /** Generic submit — works for either form mode */
  async submitForm() {
    const dialog = this.page.locator('[role="dialog"]')
    // Try create first, then update
    const createBtn = dialog.getByRole('button', { name: 'Create Product' })
    const updateBtn = dialog.getByRole('button', { name: 'Update Product' })
    const createVisible = await createBtn.isVisible().catch(() => false)
    if (createVisible) {
      await createBtn.click()
    } else {
      await updateBtn.click()
    }
  }

  /** Confirm the activate alert dialog */
  async confirmActivate() {
    await this.page.getByRole('alertdialog').getByRole('button', { name: 'Activate' }).click()
  }

  /** Confirm the deactivate alert dialog */
  async confirmDeactivate() {
    await this.page.getByRole('alertdialog').getByRole('button', { name: 'Deactivate' }).click()
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
