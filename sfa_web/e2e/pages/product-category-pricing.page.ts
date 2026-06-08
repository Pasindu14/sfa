import { type Page, type Locator, expect } from '@playwright/test'

type PriceField = 'priceA' | 'priceB' | 'priceC' | 'priceD'

const PRICE_COLUMN_INDEX: Record<PriceField, number> = {
  priceA: 3,
  priceB: 4,
  priceC: 5,
  priceD: 6,
}

export class ProductCategoryPricingPage {
  readonly saveAllButton: Locator
  readonly table: Locator

  constructor(readonly page: Page) {
    this.saveAllButton = page.getByRole('button', { name: 'Save All' })
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
    const colIndex = PRICE_COLUMN_INDEX[field]
    const row = this.getRowByCode(productCode)
    // td index is 1-based via nth-child; colIndex matches column position
    return row.locator(`td:nth-child(${colIndex}) input`)
  }

  async setPrice(productCode: string, field: PriceField, value: number) {
    const input = await this.getPriceInput(productCode, field)
    await input.click({ clickCount: 3 })
    await input.fill(String(value))
  }

  async getFirstRowCode(): Promise<string> {
    const codeCell = this.table.locator('tbody tr').first().locator('td').first().locator('span')
    return ((await codeCell.textContent()) ?? '').trim()
  }

  async getPriceValue(productCode: string, field: PriceField): Promise<number> {
    const input = await this.getPriceInput(productCode, field)
    return parseFloat((await input.inputValue()) ?? '0')
  }

  // ─── Save bar ─────────────────────────────────────────

  async clickSaveAll() {
    await this.saveAllButton.click()
    await this.page.waitForLoadState('networkidle')
  }

  async expectDirtyBadge(count: number) {
    const text = count === 1 ? '1 unsaved change' : `${count} unsaved changes`
    await expect(this.page.getByText(text).first()).toBeVisible({ timeout: 5_000 })
  }

  // ─── Assertions ───────────────────────────────────────

  async expectSuccessToast(partialText?: string) {
    const toast = this.page.locator('[data-sonner-toast][data-type="success"]').first()
    await expect(toast).toBeVisible({ timeout: 10_000 })
    if (partialText) await expect(toast).toContainText(partialText)
  }
}
