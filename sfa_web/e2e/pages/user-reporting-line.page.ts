import { type Page, type Locator, expect } from '@playwright/test'

const API_BASE = process.env.SFA_API_DOMAIN ?? 'http://localhost:5086'

export interface ReportingLineTestUsers {
  subordinateName: string
  managerName: string
  managerRole: string
}

export class UserReportingLinePage {
  readonly addButton: Locator
  readonly searchInput: Locator
  readonly table: Locator

  constructor(readonly page: Page) {
    this.addButton = page.getByRole('button', { name: 'Add Reporting Line' })
    this.searchInput = page.getByPlaceholder('Search by name…')
    this.table = page.locator('table')
  }

  // ─── Navigation ────────────────────────────────────────

  async goto() {
    await this.page.goto('/user-reporting-lines')
    await this.page.waitForLoadState('networkidle')
  }

  // ─── API helpers ───────────────────────────────────────

  /** Fetches first eligible subordinate + manager from the API for form filling */
  async getTestUsers(): Promise<ReportingLineTestUsers> {
    const sessionResp = await this.page.request.get('/api/auth/session')
    const session = await sessionResp.json()
    const token: string | undefined = session?.user?.accessToken
    if (!token) throw new Error('No accessToken in session')

    const resp = await this.page.request.get(`${API_BASE}/api/v1/users?page=1&pageSize=100`, {
      headers: { Authorization: `Bearer ${token}` },
      ignoreHTTPSErrors: true,
    })
    if (!resp.ok()) throw new Error(`Failed to fetch users: HTTP ${resp.status()}`)
    const payload = await resp.json()
    const users: Array<{ id: number; name: string; role: string; isActive: boolean }> =
      payload?.data?.users ?? []

    const subordinateRoles = ['NSM', 'RSM', 'ASM', 'Supervisor', 'SalesRep']
    const managerRoles = ['NSM', 'RSM', 'ASM', 'Supervisor']

    const sub = users.find((u) => subordinateRoles.includes(u.role) && u.isActive)
    if (!sub) throw new Error('No eligible subordinate found in the database')

    const mgr = users.find((u) => managerRoles.includes(u.role) && u.isActive && u.id !== sub.id)
    if (!mgr) throw new Error('No eligible manager found in the database')

    return { subordinateName: sub.name, managerName: mgr.name, managerRole: mgr.role }
  }

  // ─── Table helpers ─────────────────────────────────────

  getRowBySubordinate(name: string): Locator {
    return this.table.locator('tr').filter({ hasText: name }).first()
  }

  async expectRowExists(name: string) {
    await expect(this.getRowBySubordinate(name)).toBeVisible({ timeout: 10_000 })
  }

  async expectRowStatus(name: string, status: 'Active' | 'Inactive') {
    await expect(
      this.table
        .locator('tr')
        .filter({ hasText: name })
        .filter({ has: this.page.getByText(status, { exact: true }) })
        .first(),
    ).toBeVisible({ timeout: 10_000 })
  }

  /** Checks via API (bypasses pagination) that an Active reporting line exists for the given subordinate name */
  async expectApiHasActiveRecordForSubordinate(name: string) {
    const sessionResp = await this.page.request.get('/api/auth/session')
    const session = await sessionResp.json()
    const token: string | undefined = session?.user?.accessToken
    if (!token) throw new Error('No accessToken in session')
    const resp = await this.page.request.get(
      `${API_BASE}/api/v1/user-reporting-lines?page=1&pageSize=50&search=${encodeURIComponent(name)}&isActive=true`,
      { headers: { Authorization: `Bearer ${token}` }, ignoreHTTPSErrors: true },
    )
    const payload = await resp.json()
    const items: Array<{ userName: string }> = payload?.data?.userReportingLines ?? []
    expect(items.some((r) => r.userName === name)).toBe(true)
  }

  async expectTableHasRows() {
    await expect(this.table.locator('tbody tr').first()).toBeVisible({ timeout: 10_000 })
  }

  // ─── Search ────────────────────────────────────────────

  async search(query: string) {
    await this.searchInput.fill(query)
    await this.searchInput.press('Enter')
    await this.page.waitForLoadState('networkidle')
  }

  // ─── Row actions ───────────────────────────────────────

  async openRowActions(name: string, filterStatus?: 'Active' | 'Inactive') {
    const row = filterStatus
      ? this.table
          .locator('tr')
          .filter({ hasText: name })
          .filter({ has: this.page.getByText(filterStatus, { exact: true }) })
          .first()
      : this.getRowBySubordinate(name)
    await row.getByRole('button', { name: 'Open menu' }).click()
  }

  async clickEditReportingLine(name: string) {
    await this.openRowActions(name)
    await this.page.getByRole('menuitem', { name: 'Edit Reporting Line' }).click()
  }

  async clickDeactivate(name: string) {
    await this.openRowActions(name, 'Active')
    await this.page.getByRole('menuitem', { name: 'Deactivate', exact: true }).click()
  }

  async clickActivate(name: string) {
    await this.openRowActions(name, 'Inactive')
    await this.page.getByRole('menuitem', { name: 'Activate', exact: true }).click()
  }

  // ─── Dialog form interactions ──────────────────────────

  async openCreateDialog() {
    await this.addButton.click()
    await expect(this.page.getByRole('heading', { name: 'Add reporting line' })).toBeVisible()
  }

  /** Select the subordinate user via AsyncSelect (requires typing to search) */
  async selectSubordinate(name: string) {
    const dialog = this.page.locator('[role="dialog"]')
    // First combobox in dialog is the subordinate AsyncSelect trigger
    await dialog.getByRole('combobox').nth(0).click()
    const searchInput = this.page.getByPlaceholder('Search user...')
    await searchInput.waitFor({ state: 'visible', timeout: 5_000 })
    await searchInput.fill(name.substring(0, 4))
    // Wait for matching option and click it
    await this.page.getByRole('option', { name, exact: false }).first().waitFor({
      state: 'visible',
      timeout: 5_000,
    })
    await this.page.getByRole('option', { name, exact: false }).first().click()
  }

  /** Select manager role from Radix Select (second combobox in dialog) */
  async selectManagerRole(role: string) {
    const dialog = this.page.locator('[role="dialog"]')
    await dialog.getByRole('combobox').nth(1).click()
    await this.page.getByRole('listbox').waitFor({ state: 'visible', timeout: 5_000 })
    await this.page.getByRole('option', { name: role, exact: true }).click()
    await this.page.getByRole('listbox').waitFor({ state: 'hidden', timeout: 3_000 }).catch(() => {})
  }

  /** Select manager user from AsyncSelect (third combobox — options preload after role is chosen) */
  async selectManagerUser(name: string) {
    const dialog = this.page.locator('[role="dialog"]')
    await dialog.getByRole('combobox').nth(2).click()
    const searchInput = this.page.getByPlaceholder('Search manager...')
    await searchInput.waitFor({ state: 'visible', timeout: 5_000 })
    // Options preload immediately — wait for the specific name
    await this.page.getByRole('option', { name, exact: false }).first().waitFor({
      state: 'visible',
      timeout: 5_000,
    })
    await this.page.getByRole('option', { name, exact: false }).first().click()
  }

  /** Select manager role in the EDIT dialog (nth(0) — no subordinate combobox) */
  async selectManagerRoleInEdit(role: string) {
    const dialog = this.page.locator('[role="dialog"]')
    await dialog.getByRole('combobox').nth(0).click()
    await this.page.getByRole('listbox').waitFor({ state: 'visible', timeout: 5_000 })
    await this.page.getByRole('option', { name: role, exact: true }).click()
    await this.page.getByRole('listbox').waitFor({ state: 'hidden', timeout: 3_000 }).catch(() => {})
  }

  /** Select manager user in the EDIT dialog (nth(1) — no subordinate combobox) */
  async selectManagerUserInEdit(name: string) {
    const dialog = this.page.locator('[role="dialog"]')
    await dialog.getByRole('combobox').nth(1).click()
    const searchInput = this.page.getByPlaceholder('Search manager...')
    await searchInput.waitFor({ state: 'visible', timeout: 5_000 })
    await this.page.getByRole('option', { name, exact: false }).first().waitFor({ state: 'visible', timeout: 5_000 })
    await this.page.getByRole('option', { name, exact: false }).first().click()
  }

  async submitForm() {
    await this.page.getByRole('button', { name: 'Save reporting line' }).click()
  }

  async confirmAlertAction(buttonName: 'Activate' | 'Deactivate') {
    await this.page.getByRole('alertdialog').getByRole('button', { name: buttonName }).click()
  }

  // ─── Assertions ────────────────────────────────────────

  async expectSuccessToast(partialText?: string) {
    const toast = this.page.locator('[data-sonner-toast][data-type="success"]').first()
    await expect(toast).toBeVisible({ timeout: 10_000 })
    if (partialText) await expect(toast).toContainText(partialText)
  }

  async expectDialogClosed() {
    await expect(this.page.locator('[role="dialog"]')).not.toBeAttached({ timeout: 15_000 })
  }
}
