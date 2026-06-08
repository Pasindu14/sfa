import { test, expect } from '@playwright/test'

// Override admin storageState — this entire file runs as the distributor
test.use({ storageState: 'playwright/.auth/distributor.json' })

const DISTRIBUTOR_LIST_URL = '/distributor-purchase-orders'
const DISTRIBUTOR_NEW_URL = '/distributor-purchase-orders/new'
const API_BASE = process.env.SFA_API_DOMAIN ?? 'http://localhost:5086'

// ── Helpers ────────────────────────────────────────────────────────────────

async function getToken(page: import('@playwright/test').Page): Promise<string> {
  const resp = await page.request.get('/api/auth/session')
  const session = await resp.json()
  const token: string | undefined = session?.user?.accessToken
  if (!token) throw new Error('No accessToken in session')
  return token
}

async function apiPost(page: import('@playwright/test').Page, path: string, token: string) {
  const resp = await page.request.post(`${API_BASE}${path}`, {
    headers: { Authorization: `Bearer ${token}` },
    ignoreHTTPSErrors: true,
  })
  if (!resp.ok()) throw new Error(`API POST ${path} failed: HTTP ${resp.status()}`)
  return resp.json()
}

// ── List page tests ────────────────────────────────────────────────────────

test.describe('Distributor Purchase Order List', () => {
  test('shows heading "My Purchase Orders"', async ({ page }) => {
    await page.goto(DISTRIBUTOR_LIST_URL)
    await page.waitForLoadState('networkidle')
    await expect(page.getByRole('heading', { name: 'My Purchase Orders' })).toBeVisible()
  })

  test('shows KPI cards', async ({ page }) => {
    await page.goto(DISTRIBUTOR_LIST_URL)
    await page.waitForLoadState('networkidle')
    await expect(page.getByText('In Review', { exact: true })).toBeVisible()
    await expect(page.getByText('Total Orders', { exact: true })).toBeVisible()
    await expect(page.getByText('Pending Acknowledgement', { exact: true })).toBeVisible()
    await expect(page.getByText('Finalized', { exact: true }).first()).toBeVisible()
  })

  test('has New Order button that navigates to create page', async ({ page }) => {
    await page.goto(DISTRIBUTOR_LIST_URL)
    await page.waitForLoadState('networkidle')
    await page.getByRole('button', { name: 'New Order' }).click()
    await expect(page).toHaveURL(/\/distributor-purchase-orders\/new/, { timeout: 10_000 })
  })

  test('shows the orders table with search input', async ({ page }) => {
    await page.goto(DISTRIBUTOR_LIST_URL)
    await page.waitForLoadState('networkidle')
    await expect(page.getByPlaceholder('Search by order number...')).toBeVisible()
  })
})

// ── Create page tests ──────────────────────────────────────────────────────

test.describe('Distributor Purchase Order Create Page', () => {
  test('shows heading "Create Purchase Order" and action buttons', async ({ page }) => {
    await page.goto(DISTRIBUTOR_NEW_URL)
    await page.waitForLoadState('networkidle')
    await expect(page.getByRole('heading', { name: 'Create Purchase Order' })).toBeVisible()
    await expect(page.getByRole('button', { name: 'Save as Draft' })).toBeVisible()
    await expect(page.getByRole('button', { name: 'Submit for Approval' })).toBeVisible()
  })

  test('does not show a distributor selector (resolved from JWT)', async ({ page }) => {
    await page.goto(DISTRIBUTOR_NEW_URL)
    await page.waitForLoadState('networkidle')
    // Distributor portal has no distributor dropdown — it shows a read-only name
    const distributorSelect = page.getByRole('combobox').filter({ hasText: 'Select distributor...' })
    await expect(distributorSelect).toHaveCount(0)
  })
})

// ── Full workflow: Create → Submit → Finalize ─────────────────────────────

let createdOrderId = 0
let createdOrderNumber = ''

test.describe.serial('Distributor Purchase Order Workflow', () => {
  test.setTimeout(120_000)

  test('creates a draft purchase order', async ({ page }) => {
    await page.goto(DISTRIBUTOR_NEW_URL)
    await page.waitForLoadState('networkidle')

    // Select the first available product
    const productTrigger = page.getByRole('combobox').filter({ hasText: 'Select product...' }).first()
    await productTrigger.click()
    await page.locator('[role="listbox"]').waitFor({ state: 'visible', timeout: 10_000 })
    await page.locator('[role="option"]').first().click()
    await page.locator('[role="listbox"]').waitFor({ state: 'hidden', timeout: 5_000 }).catch(() => {})

    // Save as Draft
    await page.getByRole('button', { name: 'Save as Draft' }).click()
    await page.waitForLoadState('networkidle')

    // Should navigate to detail page
    await expect(page).toHaveURL(/\/distributor-purchase-orders\/\d+/, { timeout: 15_000 })

    // Capture the order ID from URL
    const url = page.url()
    const match = url.match(/\/distributor-purchase-orders\/(\d+)/)
    if (match) createdOrderId = Number(match[1])
    expect(createdOrderId).toBeGreaterThan(0)

    // Capture order number from heading
    const h1 = page.locator('h1').first()
    const text = await h1.textContent()
    createdOrderNumber = (text ?? '').trim()
    expect(createdOrderNumber).toMatch(/^PO-/)
  })

  test('submits draft order for approval from detail page', async ({ page }) => {
    if (!createdOrderId) {
      test.skip(true, 'No order created — previous test may have failed')
      return
    }

    await page.goto(`/distributor-purchase-orders/${createdOrderId}`)
    await page.waitForLoadState('networkidle')

    // Click "Submit for Approval"
    await page.getByRole('button', { name: 'Submit for Approval' }).click()

    // Confirm in the AlertDialog
    await page.getByRole('alertdialog').waitFor({ state: 'visible', timeout: 5_000 })
    await page.getByRole('button', { name: 'Submit' }).click()

    // Verify toast
    const toast = page.locator('[data-sonner-toast][data-type="success"]').first()
    await expect(toast).toBeVisible({ timeout: 10_000 })
    await expect(toast).toContainText('Order submitted for approval')
  })

  test('order shows Pending Rep Approval status in list', async ({ page }) => {
    if (!createdOrderNumber) {
      test.skip(true, 'No order number captured')
      return
    }

    await page.goto(DISTRIBUTOR_LIST_URL)
    await page.waitForLoadState('networkidle')

    const searchInput = page.getByPlaceholder('Search by order number...')
    await searchInput.fill(createdOrderNumber)
    await searchInput.press('Enter')
    await page.waitForLoadState('networkidle')

    const row = page.locator('table tr').filter({ hasText: createdOrderNumber })
    await expect(row).toBeVisible({ timeout: 10_000 })
    await expect(row).toContainText(/Pending Rep Approval/i)
  })

  test('finalizes order after admin advances it through approval', async ({ page, browser }) => {
    if (!createdOrderId) {
      test.skip(true, 'No order created — previous test may have failed')
      return
    }

    // ── Step 1: Get admin token via a separate browser context ──────────────
    const adminContext = await browser.newContext({ storageState: 'playwright/.auth/admin.json' })
    const adminPage = await adminContext.newPage()
    try {
      const adminToken = await getToken(adminPage)

      // Rep-approve
      await apiPost(adminPage, `/api/v1/purchase-orders/${createdOrderId}/rep-approve`, adminToken)
      // Manager approve → puts order into PendingDistributorFinalization
      await apiPost(adminPage, `/api/v1/purchase-orders/${createdOrderId}/approve`, adminToken)
    } finally {
      await adminContext.close()
    }

    // ── Step 2: Distributor finalizes from the detail page ──────────────────
    await page.goto(`/distributor-purchase-orders/${createdOrderId}`)
    await page.waitForLoadState('networkidle')

    // "Finalize Order" button should now be visible
    await expect(page.getByRole('button', { name: 'Finalize Order' })).toBeVisible({ timeout: 10_000 })
    await page.getByRole('button', { name: 'Finalize Order' }).click()

    // Confirm in AlertDialog
    await page.getByRole('alertdialog').waitFor({ state: 'visible', timeout: 5_000 })
    await page.getByRole('button', { name: 'Finalize' }).click()

    // Verify toast
    const toast = page.locator('[data-sonner-toast][data-type="success"]').first()
    await expect(toast).toBeVisible({ timeout: 10_000 })
    await expect(toast).toContainText('Order finalized successfully')
  })
})
