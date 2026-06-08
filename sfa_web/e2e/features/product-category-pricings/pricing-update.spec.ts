import { test, expect } from '@playwright/test'
import { ProductCategoryPricingPage } from '../../pages/product-category-pricing.page'

test.describe.serial('Product Category Pricing — Save', () => {
  test.setTimeout(60_000)

  test('should show unsaved-changes badge when a price is edited', async ({ page }) => {
    const pricingPage = new ProductCategoryPricingPage(page)
    await pricingPage.goto()
    await pricingPage.expectTableHasRows()

    const code = await pricingPage.getFirstRowCode()
    const originalValue = await pricingPage.getPriceValue(code, 'priceA')
    const newValue = originalValue + 1

    await pricingPage.setPrice(code, 'priceA', newValue)

    await pricingPage.expectDirtyBadge(1)
  })

  test('should save all prices and show success toast', async ({ page }) => {
    const pricingPage = new ProductCategoryPricingPage(page)
    await pricingPage.goto()
    await pricingPage.expectTableHasRows()

    const code = await pricingPage.getFirstRowCode()
    const originalValue = await pricingPage.getPriceValue(code, 'priceA')
    const newValue = originalValue + 1

    await pricingPage.setPrice(code, 'priceA', newValue)
    await pricingPage.expectDirtyBadge(1)

    await pricingPage.clickSaveAll()

    await pricingPage.expectSuccessToast('Pricing saved successfully')
  })

  test('should persist the saved price after page reload', async ({ page }) => {
    const pricingPage = new ProductCategoryPricingPage(page)
    await pricingPage.goto()
    await pricingPage.expectTableHasRows()

    const code = await pricingPage.getFirstRowCode()
    const originalValue = await pricingPage.getPriceValue(code, 'priceA')
    const savedValue = originalValue + 2

    await pricingPage.setPrice(code, 'priceA', savedValue)
    await pricingPage.clickSaveAll()
    await pricingPage.expectSuccessToast()

    // Reload and verify the value persisted
    await page.reload()
    await page.waitForLoadState('networkidle')

    const persistedValue = await pricingPage.getPriceValue(code, 'priceA')
    expect(persistedValue).toBe(savedValue)
  })
})
