import { test, expect } from '@playwright/test'
import { PurchaseOrderPage } from '../../pages/purchase-order.page'

let createdOrderNumber = ''

test.describe.serial('Create Purchase Order', () => {
  test.setTimeout(90_000)

  test('should display the create purchase order page', async ({ page }) => {
    const poPage = new PurchaseOrderPage(page)
    await poPage.gotoNew()
    await expect(page.getByRole('heading', { name: 'Create Purchase Order' })).toBeVisible()
    await expect(page.getByRole('button', { name: 'Save as Draft' })).toBeVisible()
    await expect(page.getByRole('button', { name: 'Submit for Approval' })).toBeVisible()
  })

  test('should not save draft when no product is selected', async ({ page }) => {
    const poPage = new PurchaseOrderPage(page)
    await poPage.gotoNew()
    // Save as Draft is disabled with no valid product selected
    const saveDraftBtn = page.getByRole('button', { name: 'Save as Draft' })
    // Button should be disabled (hasValidItem = false on load)
    await expect(saveDraftBtn).toBeDisabled()
  })

  test('should save a draft after selecting a distributor and product', async ({ page }) => {
    const poPage = new PurchaseOrderPage(page)
    await poPage.gotoNew()

    // Admin must select a distributor — fetch the first active one from the API
    await poPage.selectFirstDistributorFromApi()

    // Select the first available product in row 1
    await poPage.selectFirstProduct()

    await poPage.clickSaveDraft()

    // After save, app navigates to the detail page
    await poPage.expectOnDetailPage()

    // Capture order number from h1 on the detail page
    const heading = page.locator('h1').first()
    const text = await heading.textContent()
    createdOrderNumber = (text ?? '').trim()
  })

  test('should display the new draft order in the list', async ({ page }) => {
    const poPage = new PurchaseOrderPage(page)
    await poPage.goto()

    if (!createdOrderNumber) {
      test.skip(true, 'No order number captured — previous test may have failed')
      return
    }

    await poPage.search(createdOrderNumber)
    await poPage.expectRowExists(createdOrderNumber)
    // New order should have "Draft" status
    const row = poPage.getRowByOrderNumber(createdOrderNumber)
    await expect(row.getByText('Draft')).toBeVisible()
  })
})
