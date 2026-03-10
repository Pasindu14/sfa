import { test, expect } from '@playwright/test'
import { TerritoryPage } from '../../pages/territory.page'

test.describe('Territory List', () => {
  let territoryPage: TerritoryPage

  test.beforeEach(async ({ page }) => {
    territoryPage = new TerritoryPage(page)
    await territoryPage.goto()
  })

  test('should display the page heading', async () => {
    await expect(territoryPage.page.getByRole('heading', { name: 'Territory Management' })).toBeVisible()
  })

  test('should display territories table with correct column headers', async () => {
    await territoryPage.expectTableHasRows()
    await expect(territoryPage.page.getByRole('columnheader', { name: 'Name' })).toBeVisible()
    await expect(territoryPage.page.getByRole('columnheader', { name: 'Area' })).toBeVisible()
    await expect(territoryPage.page.getByRole('columnheader', { name: 'Status' })).toBeVisible()
    await expect(territoryPage.page.getByRole('columnheader', { name: 'Created' })).toBeVisible()
    await expect(territoryPage.page.getByRole('columnheader', { name: 'Actions' })).toBeVisible()
  })

  test('should display Add Territory button', async () => {
    await expect(territoryPage.addButton).toBeVisible()
  })

  test('should display search input', async () => {
    await expect(territoryPage.searchInput).toBeVisible()
  })

  test('should search territories by name', async () => {
    await territoryPage.expectTableHasRows()

    const firstRowName = await territoryPage.table
      .locator('tbody tr')
      .first()
      .locator('span.text-sm.font-medium')
      .textContent()

    if (firstRowName) {
      await territoryPage.search(firstRowName.trim())
      await territoryPage.expectRowExists(firstRowName.trim())
    }
  })

  test('should show empty state after searching for nonexistent territory', async () => {
    await territoryPage.search('zzz_nonexistent_territory_xyz_99999')
    await territoryPage.page.waitForTimeout(500)

    const rows = territoryPage.table.locator('tbody tr')
    const count = await rows.count()
    // All visible rows should either be 0 or a "no results" placeholder row
    expect(count).toBeLessThanOrEqual(1)
  })

  test('should restore table data after clearing search', async () => {
    await territoryPage.search('zzz_nonexistent_xyz')
    await territoryPage.page.waitForTimeout(500)
    await territoryPage.clearSearch()
    await territoryPage.expectTableHasRows()
  })
})
