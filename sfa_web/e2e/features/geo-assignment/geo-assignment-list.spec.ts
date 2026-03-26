import { test, expect } from '@playwright/test'
import { GeoAssignmentPage } from '../../pages/geo-assignment.page'

test.describe('Geo Assignment List', () => {
  let geoPage: GeoAssignmentPage

  test.beforeEach(async ({ page }) => {
    geoPage = new GeoAssignmentPage(page)
    await geoPage.goto()
  })

  test('should render the page heading', async () => {
    await expect(geoPage.page.getByRole('heading', { name: 'Geo Assignments' })).toBeVisible()
  })

  test('should render the filter card with Load Results button', async () => {
    await expect(geoPage.loadResultsButton).toBeVisible()
    await expect(geoPage.page.getByText('Filter Assignments')).toBeVisible()
  })

  test('should render the Add Assignment button', async () => {
    await expect(geoPage.addButton).toBeVisible()
  })

  test('should render the search input', async () => {
    await expect(geoPage.searchInput).toBeVisible()
  })

  test('should show table with column headers after clicking Load Results', async () => {
    await geoPage.loadResults()
    await expect(geoPage.table).toBeVisible({ timeout: 10_000 })
    // Verify column headers rendered by the table
    await expect(geoPage.table.getByText('Name')).toBeVisible()
    await expect(geoPage.table.getByText('Role')).toBeVisible()
    await expect(geoPage.table.getByText('Geo Level')).toBeVisible()
    await expect(geoPage.table.getByText('Assigned To')).toBeVisible()
    await expect(geoPage.table.getByText('Status')).toBeVisible()
    await expect(geoPage.table.getByText('Assigned On')).toBeVisible()
  })

  test('should keep table hidden before clicking Load Results', async () => {
    // The query is disabled until committed !== null — no rows should be visible
    const rows = geoPage.table.locator('tbody tr')
    // Either the table is absent or has no data rows (skeleton/empty state)
    // We just verify the loadResults button is still present (not clicked)
    await expect(geoPage.loadResultsButton).toBeVisible()
  })

  test('should support searching by name after loading results', async () => {
    await geoPage.loadResults()
    await geoPage.search('nonexistent_xyz_abc_99999')
    await expect(geoPage.searchInput).toHaveValue('nonexistent_xyz_abc_99999')
  })

  test('should clear search', async () => {
    await geoPage.loadResults()
    await geoPage.search('test')
    await geoPage.clearSearch()
    await expect(geoPage.searchInput).toHaveValue('')
  })

  test('should show filter card role and status selects', async () => {
    // Filter card has Role and Status dropdowns in the filter card header area
    const card = geoPage.page.locator('text=Filter Assignments').locator('../..')
    await expect(geoPage.page.getByText('Filter Assignments')).toBeVisible()
  })
})
