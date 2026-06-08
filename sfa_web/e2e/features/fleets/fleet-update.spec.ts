import { test, expect } from '@playwright/test'
import { FleetPage } from '../../pages/fleet.page'

const uid = Date.now().toString(36)
const ORIGINAL_NAME = `E2E Fleet Upd ${uid}`
const UPDATED_NAME = `E2E Fleet Upd ${uid} EDITED`

test.describe.serial('Update Fleet', () => {
  test('setup — create a fleet to edit', async ({ page }) => {
    const fleetPage = new FleetPage(page)
    await fleetPage.goto()
    await fleetPage.openCreateDialog()
    await fleetPage.fillForm(ORIGINAL_NAME)
    await fleetPage.submitCreateForm()
    await fleetPage.expectSuccessToast('Fleet created successfully')
    await fleetPage.expectDialogClosed()
  })

  test('opens Edit dialog with existing name pre-filled', async ({ page }) => {
    const fleetPage = new FleetPage(page)
    await fleetPage.goto()
    await fleetPage.search(ORIGINAL_NAME)
    await fleetPage.clickEdit(ORIGINAL_NAME)
    await expect(page.getByRole('heading', { name: 'Edit Fleet' })).toBeVisible()
    const dialog = page.locator('[role="dialog"]')
    await expect(dialog.getByPlaceholder('Enter fleet name')).toHaveValue(ORIGINAL_NAME)
  })

  test('updates the fleet name successfully', async ({ page }) => {
    const fleetPage = new FleetPage(page)
    await fleetPage.goto()
    await fleetPage.search(ORIGINAL_NAME)
    await fleetPage.clickEdit(ORIGINAL_NAME)
    await fleetPage.fillForm(UPDATED_NAME)
    await fleetPage.submitEditForm()
    await fleetPage.expectSuccessToast('Fleet updated successfully')
    await fleetPage.expectDialogClosed()
  })

  test('updated name appears in the list', async ({ page }) => {
    const fleetPage = new FleetPage(page)
    await fleetPage.goto()
    await fleetPage.search(UPDATED_NAME)
    await fleetPage.expectRowExists(UPDATED_NAME)
  })
})
