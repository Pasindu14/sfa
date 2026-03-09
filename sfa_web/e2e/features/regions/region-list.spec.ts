import { test, expect } from '@playwright/test'
import { RegionPage } from '../../pages/region.page'

test.describe('Region List', () => {
  let regionPage: RegionPage

  test.beforeEach(async ({ page }) => {
    regionPage = new RegionPage(page)
    await regionPage.goto()
  })

  test('should display the page heading', async () => {
    await expect(regionPage.page.getByRole('heading', { name: 'Region Management' })).toBeVisible()
  })

  test('should display regions table with correct column headers', async () => {
    await regionPage.expectTableHasRows()
    await expect(regionPage.page.getByRole('columnheader', { name: 'Name' })).toBeVisible()
    await expect(regionPage.page.getByRole('columnheader', { name: 'Status' })).toBeVisible()
    await expect(regionPage.page.getByRole('columnheader', { name: 'Created' })).toBeVisible()
    await expect(regionPage.page.getByRole('columnheader', { name: 'Actions' })).toBeVisible()
  })

  test('should display Add Region button', async () => {
    await expect(regionPage.addButton).toBeVisible()
  })

  test('should display search input', async () => {
    await expect(regionPage.searchInput).toBeVisible()
  })

  test('should search regions by name', async () => {
    await regionPage.expectTableHasRows()

    const firstRowName = await regionPage.table
      .locator('tbody tr')
      .first()
      .locator('span.text-sm.font-medium')
      .textContent()

    if (firstRowName) {
      await regionPage.search(firstRowName.trim())
      await regionPage.expectRowExists(firstRowName.trim())
    }
  })

  test('should show empty state after searching for nonexistent region', async () => {
    await regionPage.search('zzz_nonexistent_region_xyz_99999')
    await regionPage.page.waitForTimeout(500)

    const rows = regionPage.table.locator('tbody tr')
    const count = await rows.count()
    // All visible rows should either be 0 or a "no results" placeholder row
    expect(count).toBeLessThanOrEqual(1)
  })

  test('should restore table data after clearing search', async () => {
    await regionPage.search('zzz_nonexistent_xyz')
    await regionPage.page.waitForTimeout(500)
    await regionPage.clearSearch()
    await regionPage.expectTableHasRows()
  })
})
