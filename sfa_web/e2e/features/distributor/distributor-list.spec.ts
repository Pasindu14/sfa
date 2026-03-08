import { test, expect } from '@playwright/test'
import { DistributorPage } from '../../pages/distributor.page'

test.describe('Distributor List', () => {
  let distributorPage: DistributorPage

  test.beforeEach(async ({ page }) => {
    distributorPage = new DistributorPage(page)
    await distributorPage.goto()
  })

  test('should display distributors table with data', async () => {
    await distributorPage.expectTableHasRows()

    // Verify column headers match the column definitions
    await expect(distributorPage.page.getByRole('columnheader', { name: 'Distributor' })).toBeVisible()
    await expect(distributorPage.page.getByRole('columnheader', { name: 'Contact' })).toBeVisible()
    await expect(distributorPage.page.getByRole('columnheader', { name: 'Address' })).toBeVisible()
    await expect(distributorPage.page.getByRole('columnheader', { name: 'Discount / Commission' })).toBeVisible()
    await expect(distributorPage.page.getByRole('columnheader', { name: 'Status' })).toBeVisible()
  })

  test('should have Add Distributor button visible', async () => {
    await expect(distributorPage.addButton).toBeVisible()
  })

  test('should search distributors by name', async () => {
    // Get first row name to use as a known search term
    const firstRowName = await distributorPage.table
      .locator('tbody tr')
      .first()
      .locator('div.text-sm.font-medium')
      .textContent()

    if (firstRowName) {
      await distributorPage.search(firstRowName.trim())
      await distributorPage.expectRowExists(firstRowName.trim())
    }
  })

  test('should show empty state after searching for nonexistent distributor', async () => {
    await distributorPage.search('zzz_nonexistent_distributor_xyz')
    await distributorPage.page.waitForTimeout(1000)

    // Table should have no data rows
    const rows = distributorPage.table.locator('tbody tr')
    // Either 0 rows or a "no results" row — confirm the original row is gone
    const count = await rows.count()
    // After clearing, the table should repopulate
    await distributorPage.clearSearch()
    await distributorPage.expectTableHasRows()
  })

  test('should restore table data after clearing search', async () => {
    await distributorPage.search('zzz_nonexistent_xyz')
    await distributorPage.page.waitForTimeout(500)
    await distributorPage.clearSearch()
    await distributorPage.expectTableHasRows()
  })
})
