import { type Page, type Locator, expect } from '@playwright/test'

type PriceField = 'priceA' | 'priceB' | 'priceC' | 'priceD'

// td positions are 1-based. Column 1 is the gutter holding the per-row revert
// control, so the price lanes start at 4.
const PRICE_COLUMN_INDEX: Record<PriceField, number> = {
  priceA: 4,
  priceB: 5,
  priceC: 6,
  priceD: 7,
}

const CODE_COLUMN_INDEX = 2

export class ProductCategoryPricingPage {
  readonly saveButton: Locator
  readonly discardButton: Locator
  readonly table: Locator

  constructor(readonly page: Page) {
    // The commit dock is always on screen; Save and Discard are disabled until
    // there is an edit to act on.
    this.saveButton = page.getByRole('button', { name: 'Save changes' })
    this.discardButton = page.getByRole('button', { name: 'Discard' })
    this.table = page.locator('table')
  }

  // ─── Navigation ────────────────────────────────────────

  async goto() {
    await this.page.goto('/product-category-pricings')
    await this.page.waitForLoadState('networkidle')
  }

  // ─── Table helpers ─────────────────────────────────────

  async expectTableHasRows() {
    await expect(this.table.locator('tbody tr').first()).toBeVisible({ timeout: 10_000 })
  }

  getRowByCode(productCode: string): Locator {
    return this.table.locator('tbody tr').filter({ has: this.page.locator('span', { hasText: productCode }) })
  }

  // ─── Price editing ────────────────────────────────────

  async getPriceInput(productCode: string, field: PriceField): Promise<Locator> {
    const row = this.getRowByCode(productCode)
    return row.locator(`td:nth-child(${PRICE_COLUMN_INDEX[field]}) input`)
  }

  async setPrice(productCode: string, field: PriceField, value: number) {
    const input = await this.getPriceInput(productCode, field)
    await input.click({ clickCount: 3 })
    await input.fill(String(value))
  }

  async getFirstRowCode(): Promise<string> {
    const codeCell = this.table
      .locator('tbody tr')
      .first()
      .locator(`td:nth-child(${CODE_COLUMN_INDEX}) span`)
    return ((await codeCell.textContent()) ?? '').trim()
  }

  async getPriceValue(productCode: string, field: PriceField): Promise<number> {
    const input = await this.getPriceInput(productCode, field)
    return parseFloat((await input.inputValue()) ?? '0')
  }

  // ─── Commit dock ──────────────────────────────────────

  async clickSave() {
    await this.saveButton.click()
    await this.page.waitForLoadState('networkidle')
  }

  async expectEditedCount(count: number) {
    const text = count === 1 ? '1 product edited' : `${count} products edited`
    await expect(this.page.getByText(text).first()).toBeVisible({ timeout: 5_000 })
  }

  async expectNoUnsavedChanges() {
    await expect(this.page.getByText('No unsaved changes')).toBeVisible({ timeout: 5_000 })
    await expect(this.saveButton).toBeDisabled()
  }

  // ─── Assertions ───────────────────────────────────────

  async expectSuccessToast(partialText?: string) {
    const toast = this.page.locator('[data-sonner-toast][data-type="success"]').first()
    await expect(toast).toBeVisible({ timeout: 10_000 })
    if (partialText) await expect(toast).toContainText(partialText)
  }
}
