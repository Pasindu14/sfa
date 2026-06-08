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
    await expect(thead.getByText('Item Description', { exact: true })).toBeVisible()
    // A/B/C/D price column headers (each rendered inside a <span>)
    await expect(thead.getByText('A').first()).toBeVisible()
    await expect(thead.getByText('B').first()).toBeVisible()
    await expect(thead.getByText('C').first()).toBeVisible()
    await expect(thead.getByText('D').first()).toBeVisible()
  })

  test('should display the Save All button', async ({ page }) => {
    const pricingPage = new ProductCategoryPricingPage(page)
    await pricingPage.goto()
    await expect(pricingPage.saveAllButton).toBeVisible()
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
