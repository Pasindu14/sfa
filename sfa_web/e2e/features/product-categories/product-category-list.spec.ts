import { test, expect } from '@playwright/test'
import { ProductCategoryPage } from '../../pages/product-category.page'

test.describe('Product Category List', () => {
  test('should display the product categories page', async ({ page }) => {
    const catPage = new ProductCategoryPage(page)
    await catPage.goto()
    await expect(page.getByRole('heading', { name: 'Product Categories' })).toBeVisible()
  })

  test('should display the categories table with column headers', async ({ page }) => {
    const catPage = new ProductCategoryPage(page)
    await catPage.goto()
    await catPage.expectTableHasRows()
    await expect(page.getByRole('columnheader', { name: 'Category' })).toBeVisible()
    await expect(page.getByRole('columnheader', { name: 'Status' })).toBeVisible()
    await expect(page.getByRole('columnheader', { name: 'Actions' })).toBeVisible()
  })

  test('should display the Add Category button', async ({ page }) => {
    const catPage = new ProductCategoryPage(page)
    await catPage.goto()
    await expect(catPage.addButton).toBeVisible()
  })

  test('should display the search input', async ({ page }) => {
    const catPage = new ProductCategoryPage(page)
    await catPage.goto()
    await expect(catPage.searchInput).toBeVisible()
  })

  test('should filter results when searching by name', async ({ page }) => {
    const catPage = new ProductCategoryPage(page)
    await catPage.goto()
    await catPage.expectTableHasRows()

    // Name lives in div.text-sm.font-medium inside the first Category cell
    const name = (await catPage.table
      .locator('tbody tr').first()
      .locator('div.text-sm.font-medium').first()
      .textContent())?.trim() ?? ''
    test.skip(!name, 'Table appears empty — skipping search test')

    await catPage.search(name)
    await catPage.expectRowExists(name)
  })

  test('should restore results when search is cleared', async ({ page }) => {
    const catPage = new ProductCategoryPage(page)
    await catPage.goto()

    const name = (await catPage.table
      .locator('tbody tr').first()
      .locator('div.text-sm.font-medium').first()
      .textContent())?.trim() ?? ''
    test.skip(!name, 'Table appears empty — skipping clear-search test')

    await catPage.search(name)
    await catPage.clearSearch()
    await catPage.expectTableHasRows()
  })

  test('should show no results for a nonsense search term', async ({ page }) => {
    const catPage = new ProductCategoryPage(page)
    await catPage.goto()

    await catPage.search('zzzNONEXISTENTQUERYzzz')
    // Table body should be empty or show a no-results row
    const rows = catPage.table.locator('tbody tr')
    const count = await rows.count()
    if (count > 0) {
      // Some tables render a "No results" row — check for it
      const noResults = page.getByText(/no results/i)
      await expect(noResults).toBeVisible({ timeout: 5_000 })
    }
  })
})
