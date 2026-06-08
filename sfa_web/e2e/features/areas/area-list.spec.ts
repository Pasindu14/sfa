import { test, expect } from '@playwright/test'
import { AreaPage } from '../../pages/area.page'

test.describe('Area List', () => {
  let areaPage: AreaPage

  test.beforeEach(async ({ page }) => {
    areaPage = new AreaPage(page)
    await areaPage.goto()
  })

  test('should display the page heading', async () => {
    await expect(areaPage.page.getByRole('heading', { name: 'Area Management' })).toBeVisible()
  })

  test('should display areas table with correct column headers', async () => {
    await areaPage.expectTableHasRows()
    await expect(areaPage.page.getByRole('columnheader', { name: 'Name' })).toBeVisible()
    await expect(areaPage.page.getByRole('columnheader', { name: 'Region' })).toBeVisible()
    await expect(areaPage.page.getByRole('columnheader', { name: 'Status' })).toBeVisible()
    await expect(areaPage.page.getByRole('columnheader', { name: 'Created' })).toBeVisible()
    await expect(areaPage.page.getByRole('columnheader', { name: 'Actions' })).toBeVisible()
  })

  test('should display Add Area button', async () => {
    await expect(areaPage.addButton).toBeVisible()
  })

  test('should display search input', async () => {
    await expect(areaPage.searchInput).toBeVisible()
  })

  test('should search areas by name', async () => {
    await areaPage.expectTableHasRows()

    const firstRowName = await areaPage.table
      .locator('tbody tr')
      .first()
      .locator('span.text-sm.font-medium')
      .textContent()

    if (firstRowName) {
      await areaPage.search(firstRowName.trim())
      await areaPage.expectRowExists(firstRowName.trim())
    }
  })

  test('should show empty state after searching for nonexistent area', async () => {
    await areaPage.search('zzz_nonexistent_area_xyz_99999')
    await expect(areaPage.table.locator('tbody tr')).toHaveCount(1)
  })

  test('should restore table data after clearing search', async () => {
    await areaPage.search('zzz_nonexistent_xyz')
    await areaPage.clearSearch()
    await areaPage.expectTableHasRows()
  })
})
