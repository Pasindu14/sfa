import { test, expect } from '@playwright/test'
import { ProductPage } from '../../pages/product.page'

test.describe('Product List', () => {
  let productPage: ProductPage

  test.beforeEach(async ({ page }) => {
    productPage = new ProductPage(page)
    await productPage.goto()
  })

  test('should display page with correct heading', async () => {
    await expect(productPage.page.getByRole('heading', { name: 'Product Management' })).toBeVisible()
  })

  test('should display table with correct column headers', async () => {
    await expect(productPage.page.getByRole('columnheader', { name: 'Product' })).toBeVisible()
    await expect(productPage.page.getByRole('columnheader', { name: 'Print Description' })).toBeVisible()
    await expect(productPage.page.getByRole('columnheader', { name: 'Pcs / Pack' })).toBeVisible()
    await expect(productPage.page.getByRole('columnheader', { name: 'Status' })).toBeVisible()
    await expect(productPage.page.getByRole('columnheader', { name: 'Actions' })).toBeVisible()
  })

  test('should have Add Product button visible', async () => {
    await expect(productPage.addButton).toBeVisible()
  })

  test('should have search input with correct placeholder', async () => {
    await expect(productPage.searchInput).toBeVisible()
    await expect(productPage.searchInput).toHaveAttribute(
      'placeholder',
      'Search by code or description...'
    )
  })

  test('should search for an existing product and show results', async () => {
    // Get any visible text from first row code cell to use as a search term
    await productPage.expectTableHasRows()
    const firstCode = await productPage.table
      .locator('tbody tr')
      .first()
      .locator('div.text-sm.font-medium')
      .textContent()

    if (firstCode) {
      await productPage.search(firstCode.trim())
      await productPage.expectRowExists(firstCode.trim())
    }
  })

  test('should show empty state when searching for nonexistent product', async () => {
    await productPage.search('zzz_nonexistent_product_xyz_999')
    await productPage.page.waitForTimeout(1000)

    // Table should have no matching data rows
    const rows = productPage.table.locator('tbody tr')
    const count = await rows.count()
    // Confirm the table reflects absence of results (0 rows or a single "no results" row)
    expect(count).toBeLessThanOrEqual(1)
  })

  test('should restore table data after clearing search', async () => {
    await productPage.search('zzz_nonexistent_product_xyz_999')
    await productPage.page.waitForTimeout(500)
    await productPage.clearSearch()
    await productPage.expectTableHasRows()
  })
})
