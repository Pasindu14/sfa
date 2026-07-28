import { test, expect } from '@playwright/test'
import { ProductCategoryPricingPage } from '../../pages/product-category-pricing.page'

test.describe('Product Category Pricing — List', () => {
  test('should display the pricing page heading', async ({ page }) => {
    const pricingPage = new ProductCategoryPricingPage(page)
    await pricingPage.goto()
    await expect(page.getByRole('heading', { name: 'Product Category Pricing' })).toBeVisible()
  })

  test('should display the pricing table with rows', async ({ page }) => {
    const pricingPage = new ProductCategoryPricingPage(page)
    await pricingPage.goto()
    await pricingPage.expectTableHasRows()
  })

  test('should display pricing table column headers', async ({ page }) => {
    const pricingPage = new ProductCategoryPricingPage(page)
    await pricingPage.goto()
    await pricingPage.expectTableHasRows()

    const thead = pricingPage.table.locator('thead')
    await expect(thead.getByText('Code', { exact: true })).toBeVisible()
    await expect(thead.getByText('Item', { exact: true })).toBeVisible()
    // A/B/C/D tier badges (each rendered inside a <span>)
    await expect(thead.getByText('A', { exact: true }).first()).toBeVisible()
    await expect(thead.getByText('B', { exact: true }).first()).toBeVisible()
    await expect(thead.getByText('C', { exact: true }).first()).toBeVisible()
    await expect(thead.getByText('D', { exact: true }).first()).toBeVisible()
  })

  test('should keep Save disabled until a price is edited', async ({ page }) => {
    const pricingPage = new ProductCategoryPricingPage(page)
    await pricingPage.goto()
    await pricingPage.expectTableHasRows()

    await expect(pricingPage.saveButton).toBeVisible()
    await pricingPage.expectNoUnsavedChanges()

    const code = await pricingPage.getFirstRowCode()
    const originalValue = await pricingPage.getPriceValue(code, 'priceA')
    await pricingPage.setPrice(code, 'priceA', originalValue + 1)

    await expect(pricingPage.saveButton).toBeEnabled()
  })

  test('should show number inputs for each price field in the first row', async ({ page }) => {
    const pricingPage = new ProductCategoryPricingPage(page)
    await pricingPage.goto()
    await pricingPage.expectTableHasRows()

    const firstRow = pricingPage.table.locator('tbody tr').first()
    const inputs = firstRow.locator('input[type="number"]')
    await expect(inputs).toHaveCount(4)
  })
})
