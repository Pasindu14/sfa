import { test, expect } from '@playwright/test'
import { ProductCategoryPricingPage } from '../../pages/product-category-pricing.page'

test.describe.serial('Product Category Pricing — Save', () => {
  test.setTimeout(60_000)

  test('should show the edited count when a price is changed', async ({ page }) => {
    const pricingPage = new ProductCategoryPricingPage(page)
    await pricingPage.goto()
    await pricingPage.expectTableHasRows()

    const code = await pricingPage.getFirstRowCode()
    const originalValue = await pricingPage.getPriceValue(code, 'priceA')
    const newValue = originalValue + 1

    await pricingPage.setPrice(code, 'priceA', newValue)

    await pricingPage.expectEditedCount(1)
  })

  test('should drop the edited count back to zero when changes are discarded', async ({ page }) => {
    const pricingPage = new ProductCategoryPricingPage(page)
    await pricingPage.goto()
    await pricingPage.expectTableHasRows()

    const code = await pricingPage.getFirstRowCode()
    const originalValue = await pricingPage.getPriceValue(code, 'priceA')

    await pricingPage.setPrice(code, 'priceA', originalValue + 1)
    await pricingPage.expectEditedCount(1)

    await pricingPage.discardButton.click()

    await pricingPage.expectNoUnsavedChanges()
    expect(await pricingPage.getPriceValue(code, 'priceA')).toBe(originalValue)
  })

  test('should save edited prices and show success toast', async ({ page }) => {
    const pricingPage = new ProductCategoryPricingPage(page)
    await pricingPage.goto()
    await pricingPage.expectTableHasRows()

    const code = await pricingPage.getFirstRowCode()
    const originalValue = await pricingPage.getPriceValue(code, 'priceA')
    const newValue = originalValue + 1

    await pricingPage.setPrice(code, 'priceA', newValue)
    await pricingPage.expectEditedCount(1)

    await pricingPage.clickSave()

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
    await pricingPage.clickSave()
    await pricingPage.expectSuccessToast()

    // Reload and verify the value persisted
    await page.reload()
    await page.waitForLoadState('networkidle')

    const persistedValue = await pricingPage.getPriceValue(code, 'priceA')
    expect(persistedValue).toBe(savedValue)
  })
})
