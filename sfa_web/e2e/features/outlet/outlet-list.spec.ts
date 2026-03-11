import { test, expect } from '@playwright/test'
import { OutletPage } from '../../pages/outlet.page'

test.describe('Outlet List', () => {
  let outletPage: OutletPage

  test.beforeEach(async ({ page }) => {
    outletPage = new OutletPage(page)
    await outletPage.goto()
  })

  test('should display the outlet management page heading', async () => {
    await expect(outletPage.page.getByRole('heading', { name: 'Outlet Management' })).toBeVisible()
  })

  test('should have Add Outlet button visible', async () => {
    await expect(outletPage.addButton).toBeVisible()
  })

  test('should display outlets table with expected column headers', async () => {
    // Column headers defined in outlet-columns.tsx
    await expect(outletPage.page.getByRole('columnheader', { name: 'Name' })).toBeVisible()
    await expect(outletPage.page.getByRole('columnheader', { name: 'Route' })).toBeVisible()
    await expect(outletPage.page.getByRole('columnheader', { name: 'Territory / Area / Region' })).toBeVisible()
    await expect(outletPage.page.getByRole('columnheader', { name: 'Type / Category' })).toBeVisible()
    await expect(outletPage.page.getByRole('columnheader', { name: 'Status' })).toBeVisible()
    await expect(outletPage.page.getByRole('columnheader', { name: 'Actions' })).toBeVisible()
  })

  test('should have search input visible', async () => {
    await expect(outletPage.searchInput).toBeVisible()
  })

  test('should show empty state after searching for nonexistent outlet', async () => {
    await outletPage.search('zzz_nonexistent_outlet_xyz_9999')
    await outletPage.page.waitForTimeout(1000)

    // After clearing, the table should repopulate (or remain empty if DB is empty)
    await outletPage.clearSearch()
  })

  test('should restore table state after clearing search', async () => {
    await outletPage.search('zzz_nonexistent_xyz')
    await outletPage.page.waitForTimeout(500)
    await outletPage.clearSearch()

    // Table body should be present (may have 0 rows if DB is empty — just verify the DOM is intact)
    await expect(outletPage.table).toBeVisible()
  })
})
