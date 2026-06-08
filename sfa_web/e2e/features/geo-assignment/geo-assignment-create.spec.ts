import { test, expect } from '@playwright/test'
import { GeoAssignmentPage } from '../../pages/geo-assignment.page'

const today = new Date().toISOString().split('T')[0]

test.describe('Create Geo Assignment', () => {
  let geoPage: GeoAssignmentPage

  test.beforeEach(async ({ page }) => {
    geoPage = new GeoAssignmentPage(page)
    await geoPage.goto()
  })

  test('should open create dialog when clicking Add Assignment', async () => {
    await geoPage.openCreateDialog()

    const dialog = geoPage.page.locator('[role="dialog"]')
    await expect(dialog.getByText('Add geo assignment')).toBeVisible()
    await expect(dialog.getByText('Assign a user to a geographic coverage area')).toBeVisible()
    // User combobox (AsyncSelect trigger — first combobox in the dialog) and date input
    await expect(dialog.getByRole('combobox').first()).toBeVisible()
    await expect(dialog.locator('input[type="date"]')).toBeVisible()
    await expect(dialog.getByRole('button', { name: 'Save assignment' })).toBeVisible()
  })

  test('should show validation error when submitting without a user', async () => {
    await geoPage.openCreateDialog()
    // Submit without selecting a user — effectiveFrom has a default value
    await geoPage.submitForm()
    // Dialog must stay open
    const dialog = geoPage.page.locator('[role="dialog"]')
    await expect(dialog).toBeVisible()
    // Zod schema: userId.min(1, 'User is required')
    await geoPage.expectFieldError('User is required')
  })

  test('should create a new geo assignment', async () => {
    await geoPage.openCreateDialog()

    // Select first available assignable user ('a' matches names like "Asanka")
    await geoPage.selectUser('a')

    // SalesRep users require a Division — walk Region→Area→Territory→Division cascade
    await geoPage.selectFirstDivisionInCascade()

    // Ensure the effective date is set
    await geoPage.fillEffectiveFrom(today)

    await geoPage.submitForm()

    await geoPage.expectSuccessToast('Geo assignment saved successfully')
    await geoPage.expectDialogClosed()
  })

  test('should cancel the create dialog without saving', async () => {
    await geoPage.openCreateDialog()
    // Click the Cancel button inside the dialog
    const dialog = geoPage.page.locator('[role="dialog"]')
    await dialog.getByRole('button', { name: 'Cancel' }).click()
    await geoPage.expectDialogClosed()
  })
})
