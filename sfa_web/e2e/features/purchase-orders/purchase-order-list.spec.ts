import { test, expect } from '@playwright/test'
import { PurchaseOrderPage } from '../../pages/purchase-order.page'

test.describe('Purchase Order List', () => {
  test('should display the Purchase Orders page heading', async ({ page }) => {
    const poPage = new PurchaseOrderPage(page)
    await poPage.goto()
    await expect(page.getByRole('heading', { name: 'Purchase Orders' })).toBeVisible()
  })

  test('should display KPI stat cards', async ({ page }) => {
    const poPage = new PurchaseOrderPage(page)
    await poPage.goto()
    await poPage.expectKpiCard('Pending Rep Approval')
    await poPage.expectKpiCard('Pending Finalization')
    await poPage.expectKpiCard('Pending Acknowledgement')
    await poPage.expectKpiCard('Finalized')
  })

  test('should display the New Order button', async ({ page }) => {
    const poPage = new PurchaseOrderPage(page)
    await poPage.goto()
    await expect(poPage.newOrderButton).toBeVisible()
  })

  test('should display the search input and status filter', async ({ page }) => {
    const poPage = new PurchaseOrderPage(page)
    await poPage.goto()
    await expect(poPage.searchInput).toBeVisible()
    await expect(page.getByRole('combobox').filter({ hasText: /All Statuses/i })).toBeVisible()
  })

  test('should display table column headers', async ({ page }) => {
    const poPage = new PurchaseOrderPage(page)
    await poPage.goto()
    const thead = poPage.table.locator('thead')
    await expect(thead.getByText('Order #')).toBeVisible()
    await expect(thead.getByText('Distributor')).toBeVisible()
    await expect(thead.getByText('Status')).toBeVisible()
    await expect(thead.getByText('Total Amount')).toBeVisible()
  })

  test('should navigate to New Order page when button clicked', async ({ page }) => {
    const poPage = new PurchaseOrderPage(page)
    await poPage.goto()
    await poPage.newOrderButton.click()
    await expect(page).toHaveURL(/\/purchase-orders\/new/)
    await expect(page.getByRole('heading', { name: 'Create Purchase Order' })).toBeVisible()
  })
})
