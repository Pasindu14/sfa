import { test, expect } from '@playwright/test'
import { FleetPage } from '../../pages/fleet.page'

test.describe('Fleet List', () => {
  test('shows the Fleet Management heading', async ({ page }) => {
    const fleetPage = new FleetPage(page)
    await fleetPage.goto()
    await expect(page.getByRole('heading', { name: 'Fleet Management' })).toBeVisible()
  })

  test('shows the fleets table with data', async ({ page }) => {
    const fleetPage = new FleetPage(page)
    await fleetPage.goto()
    await fleetPage.expectTableHasRows()
  })

  test('shows Fleet and Status column headers', async ({ page }) => {
    const fleetPage = new FleetPage(page)
    await fleetPage.goto()
    await expect(page.getByRole('columnheader', { name: 'Fleet' })).toBeVisible()
    await expect(page.getByRole('columnheader', { name: 'Status' })).toBeVisible()
  })

  test('shows Add Fleet button', async ({ page }) => {
    const fleetPage = new FleetPage(page)
    await fleetPage.goto()
    await expect(fleetPage.addButton).toBeVisible()
  })

  test('shows search input', async ({ page }) => {
    const fleetPage = new FleetPage(page)
    await fleetPage.goto()
    await expect(fleetPage.searchInput).toBeVisible()
  })

  test('search filters the table', async ({ page }) => {
    const fleetPage = new FleetPage(page)
    await fleetPage.goto()
    await fleetPage.expectTableHasRows()

    // Get the name from the first row
    const firstName = await fleetPage.table
      .locator('div.text-sm.font-medium')
      .first()
      .textContent()
    const query = (firstName ?? '').trim().slice(0, 4)

    await fleetPage.search(query)
    await fleetPage.expectTableHasRows()
  })

  test('each row has an actions menu', async ({ page }) => {
    const fleetPage = new FleetPage(page)
    await fleetPage.goto()
    await expect(
      fleetPage.table.locator('tbody tr').first().getByRole('button', { name: 'Open menu' })
    ).toBeVisible()
  })
})
