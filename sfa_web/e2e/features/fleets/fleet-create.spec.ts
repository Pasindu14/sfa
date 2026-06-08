import { test, expect } from '@playwright/test'
import { FleetPage } from '../../pages/fleet.page'

const uid = Date.now().toString(36)
const FLEET_NAME = `E2E Fleet ${uid}`

test.describe('Create Fleet', () => {
  test('opens the Create Fleet dialog', async ({ page }) => {
    const fleetPage = new FleetPage(page)
    await fleetPage.goto()
    await fleetPage.openCreateDialog()
    await expect(page.locator('[role="dialog"]')).toBeVisible()
  })

  test('shows validation error when name is empty', async ({ page }) => {
    const fleetPage = new FleetPage(page)
    await fleetPage.goto()
    await fleetPage.openCreateDialog()
    await fleetPage.submitCreateForm()
    await expect(page.getByText(/required|least/i).first()).toBeVisible()
  })

  test('creates a fleet successfully', async ({ page }) => {
    const fleetPage = new FleetPage(page)
    await fleetPage.goto()
    await fleetPage.openCreateDialog()
    await fleetPage.fillForm(FLEET_NAME)
    await fleetPage.submitCreateForm()
    await fleetPage.expectSuccessToast('Fleet created successfully')
    await fleetPage.expectDialogClosed()
  })

  test('new fleet appears in the list', async ({ page }) => {
    const fleetPage = new FleetPage(page)
    await fleetPage.goto()
    await fleetPage.search(FLEET_NAME)
    await fleetPage.expectRowExists(FLEET_NAME)
  })

  test('new fleet has Active status by default', async ({ page }) => {
    const fleetPage = new FleetPage(page)
    await fleetPage.goto()
    await fleetPage.search(FLEET_NAME)
    await fleetPage.expectRowStatus(FLEET_NAME, 'Active')
  })

  test('closes dialog without saving on cancel', async ({ page }) => {
    const fleetPage = new FleetPage(page)
    await fleetPage.goto()
    await fleetPage.openCreateDialog()
    await fleetPage.fillForm('Should Not Exist Fleet')
    await page.keyboard.press('Escape')
    await fleetPage.expectDialogClosed()
    await fleetPage.search('Should Not Exist Fleet')
    await expect(fleetPage.table.locator('tbody')).not.toContainText('Should Not Exist Fleet')
  })
})
