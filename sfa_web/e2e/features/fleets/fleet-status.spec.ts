import { test, expect } from '@playwright/test'
import { FleetPage } from '../../pages/fleet.page'

const uid = Date.now().toString(36)
const FLEET_NAME = `E2E Fleet Status ${uid}`

test.describe.serial('Fleet Status', () => {
  test('setup — create a fleet for status tests', async ({ page }) => {
    const fleetPage = new FleetPage(page)
    await fleetPage.goto()
    await fleetPage.openCreateDialog()
    await fleetPage.fillForm(FLEET_NAME)
    await fleetPage.submitCreateForm()
    await fleetPage.expectSuccessToast('Fleet created successfully')
    await fleetPage.expectDialogClosed()
  })

  test('new fleet is Active by default', async ({ page }) => {
    const fleetPage = new FleetPage(page)
    await fleetPage.goto()
    await fleetPage.search(FLEET_NAME)
    await fleetPage.expectRowStatus(FLEET_NAME, 'Active')
  })

  test('shows Deactivate option for an active fleet', async ({ page }) => {
    const fleetPage = new FleetPage(page)
    await fleetPage.goto()
    await fleetPage.search(FLEET_NAME)
    await fleetPage.openRowActions(FLEET_NAME)
    await expect(page.getByRole('menuitem', { name: 'Deactivate', exact: true })).toBeVisible()
    await page.keyboard.press('Escape')
  })

  test('deactivates a fleet successfully', async ({ page }) => {
    const fleetPage = new FleetPage(page)
    await fleetPage.goto()
    await fleetPage.search(FLEET_NAME)
    await fleetPage.clickDeactivate(FLEET_NAME)
    await fleetPage.confirmAlertAction('Deactivate')
    await fleetPage.expectSuccessToast('Fleet deactivated successfully')
  })

  test('fleet shows Inactive after deactivation', async ({ page }) => {
    const fleetPage = new FleetPage(page)
    await fleetPage.goto()
    await fleetPage.search(FLEET_NAME)
    await fleetPage.expectRowStatus(FLEET_NAME, 'Inactive')
  })

  test('shows Activate option for an inactive fleet', async ({ page }) => {
    const fleetPage = new FleetPage(page)
    await fleetPage.goto()
    await fleetPage.search(FLEET_NAME)
    await fleetPage.openRowActions(FLEET_NAME)
    await expect(page.getByRole('menuitem', { name: 'Activate', exact: true })).toBeVisible()
    await page.keyboard.press('Escape')
  })

  test('activates a fleet successfully', async ({ page }) => {
    const fleetPage = new FleetPage(page)
    await fleetPage.goto()
    await fleetPage.search(FLEET_NAME)
    await fleetPage.clickActivate(FLEET_NAME)
    await fleetPage.confirmAlertAction('Activate')
    await fleetPage.expectSuccessToast('Fleet activated successfully')
  })

  test('fleet shows Active after reactivation', async ({ page }) => {
    const fleetPage = new FleetPage(page)
    await fleetPage.goto()
    await fleetPage.search(FLEET_NAME)
    await fleetPage.expectRowStatus(FLEET_NAME, 'Active')
  })
})
