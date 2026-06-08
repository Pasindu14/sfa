import { test, expect } from '@playwright/test'
import { RoutePage } from '../../pages/route.page'

test.describe('Route List', () => {
  let routePage: RoutePage

  test.beforeEach(async ({ page }) => {
    routePage = new RoutePage(page)
    await routePage.goto()
  })

  test('should display the page heading', async () => {
    await expect(routePage.page.getByRole('heading', { name: 'Route Management' })).toBeVisible()
  })

  test('should display routes table with correct column headers', async () => {
    await routePage.expectTableHasRows()
    await expect(routePage.page.getByRole('columnheader', { name: 'Name' })).toBeVisible()
    await expect(routePage.page.getByRole('columnheader', { name: 'Division' })).toBeVisible()
    await expect(routePage.page.getByRole('columnheader', { name: 'Territory' })).toBeVisible()
    await expect(routePage.page.getByRole('columnheader', { name: 'Area' })).toBeVisible()
    await expect(routePage.page.getByRole('columnheader', { name: 'Region' })).toBeVisible()
    await expect(routePage.page.getByRole('columnheader', { name: 'Created' })).toBeVisible()
    await expect(routePage.page.getByRole('columnheader', { name: 'Actions' })).toBeVisible()
  })

  test('should display Add Route button', async () => {
    await expect(routePage.addButton).toBeVisible()
  })

  test('should display search input', async () => {
    await expect(routePage.searchInput).toBeVisible()
  })

  test('should search routes by name', async () => {
    await routePage.expectTableHasRows()

    const firstRowName = await routePage.table
      .locator('tbody tr')
      .first()
      .locator('span.text-sm.font-medium')
      .textContent()

    if (firstRowName) {
      await routePage.search(firstRowName.trim())
      await routePage.expectRowExists(firstRowName.trim())
    }
  })

  test('should show empty state after searching for nonexistent route', async () => {
    await routePage.search('zzz_nonexistent_route_xyz_99999')
    const rows = routePage.table.locator('tbody tr')
    await expect(rows).toHaveCount(1)
  })

  test('should restore table data after clearing search', async () => {
    await routePage.search('zzz_nonexistent_xyz')
    await routePage.clearSearch()
    await routePage.expectTableHasRows()
  })
})
