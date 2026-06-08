import { test, expect } from '@playwright/test'
import { PurchaseOrderPage } from '../../pages/purchase-order.page'

test.describe('Purchase Order Detail', () => {
  test.setTimeout(60_000)

  test('should navigate to detail page when View is clicked', async ({ page }) => {
    const poPage = new PurchaseOrderPage(page)
    await poPage.goto()
    await poPage.expectTableHasRows()

    await poPage.clickFirstOrderLink()

    await poPage.expectOnDetailPage()
  })

  test('should display order number on detail page', async ({ page }) => {
    const poPage = new PurchaseOrderPage(page)
    await poPage.goto()
    await poPage.expectTableHasRows()

    await poPage.clickFirstOrderLink()
    await poPage.expectOnDetailPage()

    // Order number should appear on the detail page (format PO-XXXXX)
    await expect(page.locator('text=/PO-\\d+/').first()).toBeVisible({ timeout: 10_000 })
  })

  test('should display Back to Purchase Orders link on detail page', async ({ page }) => {
    const poPage = new PurchaseOrderPage(page)
    await poPage.goto()
    await poPage.expectTableHasRows()

    await poPage.clickFirstOrderLink()
    await poPage.expectOnDetailPage()

    // Back navigation link should be present
    const backLink = page.getByRole('link', { name: /purchase orders/i })
    await expect(backLink).toBeVisible({ timeout: 10_000 })
  })
})
