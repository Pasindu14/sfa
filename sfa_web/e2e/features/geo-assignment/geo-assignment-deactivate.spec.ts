import { test, expect } from '@playwright/test'
import { GeoAssignmentPage } from '../../pages/geo-assignment.page'

const today = new Date().toISOString().split('T')[0]

// Shared across the serial suite — set in setup, read in subsequent tests
let createdUserName = ''

test.describe.serial('Deactivate & Activate Geo Assignment', () => {
  // Deactivate tests chain goto + loadResults + search + row actions + alertdialog assertions.
  // With slowMo:500, this can exceed the default 30s — give each test more headroom.
  test.setTimeout(60_000)
  test('setup: create a geo assignment to deactivate', async ({ page }) => {
    const geoPage = new GeoAssignmentPage(page)
    await geoPage.goto()

    await geoPage.openCreateDialog()
    // Use a different search term from update spec to reduce chance of same user collision
    createdUserName = await geoPage.selectUser('a')
    // SalesRep users require a Division — walk the full geo cascade
    await geoPage.selectFirstDivisionInCascade()
    await geoPage.fillEffectiveFrom(today)
    await geoPage.submitForm()

    await geoPage.expectSuccessToast()
    await geoPage.expectDialogClosed()
  })

  test('should open deactivate confirmation dialog', async ({ page }) => {
    const geoPage = new GeoAssignmentPage(page)
    await geoPage.goto()
    // Filter to Active only before loading — prevents the newly created row from being buried
    // on page 2 behind accumulated Inactive rows from prior test runs
    await geoPage.filterByStatus('Active')
    await geoPage.loadResults()
    await geoPage.search(createdUserName)
    await geoPage.expectRowStatus(createdUserName, 'Active')

    await geoPage.clickDeactivate(createdUserName)

    // Verify the alertdialog appears and then dismiss it (full text/cancel flow is in next test)
    await expect(page.getByRole('alertdialog')).toBeVisible({ timeout: 15_000 })
    await geoPage.cancelAlert()
  })

  test('should cancel deactivate and keep assignment Active', async ({ page }) => {
    const geoPage = new GeoAssignmentPage(page)
    await geoPage.goto()
    await geoPage.filterByStatus('Active')
    await geoPage.loadResults()
    await geoPage.search(createdUserName)
    await geoPage.expectRowStatus(createdUserName, 'Active')

    await geoPage.clickDeactivate(createdUserName)
    await geoPage.cancelAlert()

    // Assignment should still be Active after canceling
    await geoPage.expectRowStatus(createdUserName, 'Active')
  })

  test('should deactivate assignment successfully', async ({ page }) => {
    const geoPage = new GeoAssignmentPage(page)
    await geoPage.goto()
    await geoPage.loadResults()
    // Expand page size so all rows (Active + Inactive) fit on one page — allows verifying
    // the status transition from Active → Inactive without a filter hiding the row
    await geoPage.setPageSize('50')
    await geoPage.search(createdUserName)
    await geoPage.expectRowExists(createdUserName)
    await geoPage.expectRowStatus(createdUserName, 'Active')

    await geoPage.clickDeactivate(createdUserName)
    await geoPage.confirmAlertAction('Deactivate')

    await geoPage.expectSuccessToast('Geo assignment deactivated')

    // Table auto-refreshes via query invalidation — verify updated status
    await geoPage.expectRowStatus(createdUserName, 'Inactive')
  })

  test('should show Activate menu item for an inactive assignment', async ({ page }) => {
    const geoPage = new GeoAssignmentPage(page)
    await geoPage.goto()
    await geoPage.loadResults()
    await geoPage.setPageSize('50')
    await geoPage.search(createdUserName)
    await geoPage.expectRowExists(createdUserName)
    await geoPage.expectRowStatus(createdUserName, 'Inactive')

    // Target the Inactive row specifically so we see the Activate menu item
    await geoPage.openRowActions(createdUserName, 'Inactive')
    await expect(page.getByRole('menuitem', { name: 'Activate' })).toBeVisible()
    await expect(page.getByRole('menuitem', { name: 'Deactivate' })).not.toBeVisible()
  })

  test('should activate assignment successfully', async ({ page }) => {
    const geoPage = new GeoAssignmentPage(page)
    await geoPage.goto()
    await geoPage.loadResults()
    await geoPage.setPageSize('50')
    await geoPage.search(createdUserName)
    await geoPage.expectRowExists(createdUserName)

    await geoPage.clickActivate(createdUserName)
    await geoPage.confirmAlertAction('Activate')

    await geoPage.expectSuccessToast('Geo assignment activated')

    // Table auto-refreshes — verify Active badge is back
    await geoPage.expectRowStatus(createdUserName, 'Active')
  })
})
