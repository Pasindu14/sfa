import { test, expect } from '@playwright/test'
import { GeoAssignmentPage } from '../../pages/geo-assignment.page'

const today = new Date().toISOString().split('T')[0]
const tomorrow = new Date(Date.now() + 86_400_000).toISOString().split('T')[0]

// Shared across the serial suite — set in setup, read in subsequent tests
let createdUserName = ''

test.describe.serial('Update Geo Assignment', () => {
  test.setTimeout(60_000)
  test('setup: create a geo assignment to edit', async ({ page }) => {
    const geoPage = new GeoAssignmentPage(page)
    await geoPage.goto()

    await geoPage.openCreateDialog()
    createdUserName = await geoPage.selectUser('a')
    // SalesRep users require a Division — walk the full geo cascade
    await geoPage.selectFirstDivisionInCascade()
    await geoPage.fillEffectiveFrom(today)
    await geoPage.submitForm()

    await geoPage.expectSuccessToast()
    await geoPage.expectDialogClosed()
  })

  test('should open edit dialog with pre-filled user name and date', async ({ page }) => {
    const geoPage = new GeoAssignmentPage(page)
    await geoPage.goto()
    await geoPage.loadResults()
    await geoPage.search(createdUserName)
    await geoPage.expectRowExists(createdUserName)

    await geoPage.clickEditAssignment(createdUserName)

    const dialog = page.locator('[role="dialog"]')
    await expect(dialog.getByRole('heading', { name: 'Edit Geo Assignment' })).toBeVisible()
    // Edit form shows a read-only user preview card with the user's name (exact to avoid
    // matching the dialog description "Update assignment for {name}.")
    await expect(dialog.getByText(createdUserName, { exact: true })).toBeVisible()
    // The date field should be present and pre-filled
    const dateInput = dialog.locator('input[type="date"]')
    await expect(dateInput).toBeVisible()
    // Date must be a non-empty valid date string
    const dateValue = await dateInput.inputValue()
    expect(dateValue).toMatch(/^\d{4}-\d{2}-\d{2}$/)
  })

  test('should update effective date successfully', async ({ page }) => {
    const geoPage = new GeoAssignmentPage(page)
    await geoPage.goto()
    await geoPage.loadResults()
    await geoPage.search(createdUserName)
    await geoPage.expectRowExists(createdUserName)

    await geoPage.clickEditAssignment(createdUserName)
    await geoPage.fillEffectiveFrom(tomorrow)
    await geoPage.submitForm()

    await geoPage.expectSuccessToast('Geo assignment updated successfully')
    await geoPage.expectDialogClosed()
  })

  test('should show validation error when effectiveFrom is cleared', async ({ page }) => {
    const geoPage = new GeoAssignmentPage(page)
    await geoPage.goto()
    await geoPage.loadResults()
    await geoPage.search(createdUserName)
    await geoPage.expectRowExists(createdUserName)

    await geoPage.clickEditAssignment(createdUserName)

    // Clear the date field
    const dialog = page.locator('[role="dialog"]')
    await dialog.locator('input[type="date"]').fill('')
    await geoPage.submitForm()

    // Dialog should stay open; Zod error fires
    await expect(dialog).toBeVisible()
    await geoPage.expectFieldError('Effective date is required')
  })
})
