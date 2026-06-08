import { type Page, type Locator, expect } from '@playwright/test'

export class GeoAssignmentPage {
  // --- Page-level locators ---
  readonly addButton: Locator
  readonly searchInput: Locator
  readonly table: Locator
  readonly loadResultsButton: Locator

  constructor(readonly page: Page) {
    this.addButton = page.getByRole('button', { name: 'Add Assignment' })
    this.searchInput = page.getByPlaceholder('Search by name…')
    this.table = page.locator('table')
    this.loadResultsButton = page.getByRole('button', { name: 'Load Results' })
  }

  // ─── Navigation ────────────────────────────────────────

  async goto() {
    await this.page.goto('/geo-assignments')
    // 'load' is faster than 'networkidle'; row assertions use their own timeouts
    await this.page.waitForLoadState('load')
  }

  /**
   * The table is gated behind a "committed" filter state.
   * Call this before asserting any table rows.
   */
  async loadResults() {
    await this.loadResultsButton.click()
    // Short wait so the initial API query fires before we interact with the table
    await this.page.waitForTimeout(800)
  }

  // ─── Table helpers ─────────────────────────────────────

  /**
   * Returns the first tbody row that contains the given user name.
   * Using tbody (not tr) excludes the header row.
   * Using .first() avoids strict-mode violations when a user has multiple assignments.
   */
  getRowByUserName(name: string): Locator {
    return this.table.locator('tbody tr').filter({ hasText: name }).first()
  }

  /**
   * Returns the first tbody row matching both user name and status badge text.
   * Uses exact-text matching for status to avoid 'Active' matching 'Inactive' rows
   * (hasText does substring matching — 'Inactive' contains 'Active').
   */
  getRowByUserNameAndStatus(name: string, status: 'Active' | 'Inactive'): Locator {
    return this.table.locator('tbody tr')
      .filter({ hasText: name })
      .filter({ has: this.page.getByText(status, { exact: true }) })
      .first()
  }

  async expectRowExists(name: string) {
    await expect(this.getRowByUserName(name)).toBeVisible({ timeout: 10_000 })
  }

  /**
   * Asserts that at least one row exists for the user with the expected status badge.
   */
  async expectRowStatus(name: string, status: 'Active' | 'Inactive') {
    await expect(this.getRowByUserNameAndStatus(name, status)).toBeVisible({ timeout: 10_000 })
  }

  async expectTableHasRows() {
    await expect(this.table.locator('tbody tr').first()).toBeVisible({ timeout: 10_000 })
  }

  // ─── Filters (must be called before loadResults) ───────

  /**
   * Sets the Status filter in the filter card.
   * Must be called before loadResults() on a fresh page — assumes default "All status" state.
   */
  async filterByStatus(status: 'Active' | 'Inactive') {
    // The trigger placeholder text is "All status" on a fresh page
    await this.page.getByRole('combobox').filter({ hasText: 'All status' }).click()
    const optionText = status === 'Active' ? 'Active only' : 'Inactive only'
    // exact: true required — 'Inactive only' contains 'active only' as a substring (case-insensitive)
    await this.page.getByRole('option', { name: optionText, exact: true }).click()
    await this.page.locator('[data-radix-select-content]')
      .waitFor({ state: 'hidden', timeout: 3_000 }).catch(() => {})
  }

  /**
   * Changes the "Rows per page" pagination select.
   * Call after loadResults() to fit more rows onto a single page.
   * Available options: 10, 20, 30, 40, 50.
   */
  async setPageSize(size: '20' | '30' | '40' | '50') {
    await this.page.getByText('Rows per page').locator('..').getByRole('combobox').click()
    await this.page.getByRole('option', { name: size, exact: true }).click()
    await this.page.waitForTimeout(500)
  }

  // ─── Search ──────────────────────────────────────────

  async search(query: string) {
    await this.searchInput.fill(query)
    await this.page.waitForTimeout(500)
  }

  async clearSearch() {
    await this.searchInput.clear()
    await this.page.waitForTimeout(500)
  }

  // ─── Row actions (dropdown menu) ──────────────────────

  /**
   * Opens the actions dropdown for the given user.
   * Pass `status` to target a specific assignment when the same user has multiple rows.
   */
  async openRowActions(name: string, status?: 'Active' | 'Inactive') {
    const row = status
      ? this.getRowByUserNameAndStatus(name, status)
      : this.getRowByUserName(name)
    await row.getByRole('button', { name: 'Open menu' }).click()
  }

  async clickEditAssignment(name: string) {
    await this.openRowActions(name)
    await this.page.getByRole('menuitem', { name: 'Edit Assignment' }).click()
  }

  /** Targets the Active row for the user so Deactivate is always available. */
  async clickDeactivate(name: string) {
    await this.openRowActions(name, 'Active')
    await this.page.getByRole('menuitem', { name: 'Deactivate' }).click()
  }

  /** Targets the Inactive row for the user so Activate is always available. */
  async clickActivate(name: string) {
    await this.openRowActions(name, 'Inactive')
    await this.page.getByRole('menuitem', { name: 'Activate' }).click()
  }

  // ─── Dialog interactions ──────────────────────────────

  async openCreateDialog() {
    await this.addButton.click()
    await expect(this.page.getByRole('heading', { name: 'Add geo assignment' })).toBeVisible()
  }

  /**
   * Select a user via the AsyncSelect combobox.
   * Types `query` into the command input, waits for results, clicks the first option.
   * Returns the user's display name for later row assertions.
   */
  async selectUser(query: string): Promise<string> {
    const dialog = this.page.locator('[role="dialog"]')
    const combobox = dialog.getByRole('combobox').first()
    await expect(combobox).not.toBeDisabled({ timeout: 10_000 })
    await combobox.click()
    // AsyncSelect label="User" → CommandInput placeholder "Search user..."
    const searchInput = this.page.getByPlaceholder('Search user...')
    await searchInput.waitFor({ state: 'visible', timeout: 5_000 })
    await searchInput.fill(query)
    // Wait for cmdk items to load (fetcher only fires when query is non-empty)
    await this.page.locator('[cmdk-item]:not([data-disabled="true"])').first()
      .waitFor({ state: 'visible', timeout: 8_000 })
    const firstOption = this.page.locator('[cmdk-item]:not([data-disabled="true"])').first()
    await firstOption.click()
    // Wait for Popover overlay to fully leave the DOM before continuing —
    // clicking Submit while Radix's exit animation is live causes a backdrop-click
    // that closes the Dialog without submitting.
    await this.page.locator('[data-radix-popper-content-wrapper]')
      .waitFor({ state: 'hidden', timeout: 5_000 }).catch(() => {})
    // Confirm trigger shows the selected name (not the placeholder) — double-checks
    // the Popover is fully closed and the selection registered in RHF state.
    await expect(combobox).not.toContainText('Type to search', { timeout: 5_000 })
    const triggerText = await combobox.innerText()
    return triggerText.split('—')[0].trim()
  }

  async fillEffectiveFrom(date: string) {
    const dialog = this.page.locator('[role="dialog"]')
    await dialog.locator('input[type="date"]').fill(date)
  }

  async submitForm() {
    // Wait for all geo-data queries to finish (isLoadingGeo → false unblocks the button).
    await this.page.waitForLoadState('networkidle')
    const btn = this.page.locator('[role="dialog"] button[type="submit"]')
    await btn.scrollIntoViewIfNeeded()
    await btn.evaluate((el) => (el as HTMLButtonElement).click())
  }

  /**
   * Selects a specific option in a Radix Select identified by its placeholder text.
   * Waits for the combobox showing `placeholder` to appear, clicks it, then clicks
   * the option whose text exactly matches `optionName`.
   */
  private async selectCascadeOption(placeholder: string, optionName: string) {
    const dialog = this.page.locator('[role="dialog"]')
    // The combobox shows the placeholder until a value is selected; Playwright
    // auto-retries until it appears (handles React re-renders between cascade steps).
    const trigger = dialog.getByRole('combobox').filter({ hasText: placeholder })
    await trigger.click()
    await this.page.locator('[role="listbox"]').waitFor({ state: 'visible', timeout: 5_000 })
    await this.page.getByRole('option', { name: optionName, exact: true }).click()
    await this.page.locator('[role="listbox"]').waitFor({ state: 'hidden', timeout: 3_000 }).catch(() => {})
  }

  /**
   * Walks the Region → Area → Territory → Division cascade in the dialog.
   * Fetches the first active division from the .NET API (via the NextAuth session
   * token) to get a guaranteed valid geo chain; then selects each level by name.
   * Required when the selected user is a SalesRep (API enforces Division).
   */
  async selectFirstDivisionInCascade() {
    // Step 1: get the admin's Bearer token from NextAuth session
    const sessionResp = await this.page.request.get('/api/auth/session')
    const session = await sessionResp.json()
    const token: string | undefined = session?.user?.accessToken
    if (!token) throw new Error('No accessToken in NextAuth session — is auth setup complete?')

    // Step 2: fetch the first active division (which carries the full ancestor chain)
    const apiBase = process.env.SFA_API_DOMAIN ?? 'https://127.0.0.1:7169'
    const divResp = await this.page.request.get(`${apiBase}/api/v1/divisions/active`, {
      headers: { Authorization: `Bearer ${token}` },
      ignoreHTTPSErrors: true,
    })
    if (!divResp.ok()) throw new Error(`Failed to fetch divisions: HTTP ${divResp.status()}`)
    const payload = await divResp.json()
    const division = payload?.data?.[0]
    if (!division) throw new Error('No active divisions in the database')

    // Step 3: walk the cascade using exact ancestor names from the DTO
    await this.selectCascadeOption('Select region',    division.regionName)
    await this.selectCascadeOption('Select area',      division.areaName)
    await this.selectCascadeOption('Select territory', division.territoryName)
    await this.selectCascadeOption('Select division',  division.name)
  }

  async confirmAlertAction(buttonName: 'Deactivate' | 'Activate') {
    await this.page.getByRole('alertdialog').getByRole('button', { name: buttonName }).click()
  }

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
