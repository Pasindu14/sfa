import { type Page, type Locator, expect } from '@playwright/test'

export class PurchaseOrderPage {
  readonly newOrderButton: Locator
  readonly searchInput: Locator
  readonly table: Locator

  constructor(readonly page: Page) {
    this.newOrderButton = page.getByRole('button', { name: 'New Order' })
    this.searchInput = page.getByPlaceholder('Search orders...')
    this.table = page.locator('table')
  }

  // ─── Navigation ────────────────────────────────────────

  async goto() {
    await this.page.goto('/purchase-orders')
    await this.page.waitForLoadState('networkidle')
  }

  async gotoNew() {
    await this.page.goto('/purchase-orders/new')
    await this.page.waitForLoadState('networkidle')
  }

  // ─── List page helpers ────────────────────────────────

  async expectTableHasRows() {
    await expect(this.table.locator('tbody tr').first()).toBeVisible({ timeout: 10_000 })
  }

  async expectKpiCard(title: string) {
    await expect(this.page.getByText(title, { exact: true })).toBeVisible()
  }

  getRowByOrderNumber(orderNumber: string): Locator {
    return this.table.locator('tr').filter({ hasText: orderNumber })
  }

  async expectRowExists(orderNumber: string) {
    await expect(this.getRowByOrderNumber(orderNumber)).toBeVisible({ timeout: 10_000 })
  }

  async search(query: string) {
    await this.searchInput.fill(query)
    await this.searchInput.press('Enter')
    await this.page.waitForLoadState('networkidle')
  }

  async clickView(orderNumber: string) {
    const row = this.getRowByOrderNumber(orderNumber)
    await row.getByRole('button', { name: 'Open menu' }).click()
    await this.page.getByRole('menuitem', { name: 'View' }).click()
    await this.page.waitForLoadState('networkidle')
  }

  async clickFirstOrderLink() {
    // Order # column renders as a link — click the first one
    await this.table.locator('tbody tr').first().locator('a').first().click()
    await this.page.waitForLoadState('networkidle')
  }

  // ─── Create page helpers ──────────────────────────────

  async selectFirstProduct() {
    // The first line item row has a product SelectTrigger
    const productTrigger = this.page.getByRole('combobox').filter({ hasText: 'Select product...' }).first()
    await productTrigger.click()
    await this.page.locator('[role="listbox"]').waitFor({ state: 'visible', timeout: 5_000 })
    await this.page.locator('[role="option"]').first().click()
    await this.page.locator('[role="listbox"]').waitFor({ state: 'hidden', timeout: 3_000 }).catch(() => {})
  }

  async selectDistributor(name: string) {
    const trigger = this.page.getByRole('combobox').filter({ hasText: 'Select distributor...' })
    await trigger.click()
    await this.page.locator('[role="listbox"]').waitFor({ state: 'visible', timeout: 5_000 })
    await this.page.getByRole('option', { name, exact: true }).click()
    await this.page.locator('[role="listbox"]').waitFor({ state: 'hidden', timeout: 3_000 }).catch(() => {})
  }

  async selectFirstDistributorFromApi() {
    const sessionResp = await this.page.request.get('/api/auth/session')
    const session = await sessionResp.json()
    const token: string | undefined = session?.user?.accessToken
    if (!token) throw new Error('No accessToken in NextAuth session')

    const apiBase = process.env.SFA_API_DOMAIN ?? 'https://127.0.0.1:7169'
    const resp = await this.page.request.get(`${apiBase}/api/v1/distributors?page=1&pageSize=1`, {
      headers: { Authorization: `Bearer ${token}` },
      ignoreHTTPSErrors: true,
    })
    if (!resp.ok()) throw new Error(`Failed to fetch distributors: HTTP ${resp.status()}`)
    const payload = await resp.json()
    const distributor = payload?.data?.distributors?.[0]
    if (!distributor) throw new Error('No active distributors in the database')

    await this.selectDistributor(distributor.name)
  }

  async clickSaveDraft() {
    await this.page.getByRole('button', { name: 'Save as Draft' }).click()
    await this.page.waitForLoadState('networkidle')
  }

  // ─── Assertions ───────────────────────────────────────

  async expectSuccessToast(partialText?: string) {
    const toast = this.page.locator('[data-sonner-toast][data-type="success"]').first()
    await expect(toast).toBeVisible({ timeout: 10_000 })
    if (partialText) await expect(toast).toContainText(partialText)
  }

  async expectOnDetailPage() {
    await expect(this.page).toHaveURL(/\/purchase-orders\/\d+/, { timeout: 10_000 })
  }
}
