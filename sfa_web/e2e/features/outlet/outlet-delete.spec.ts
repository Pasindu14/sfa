import { test, expect } from '@playwright/test'
import { OutletPage, type OutletFormData } from '../../pages/outlet.page'

const uniqueSuffix = Date.now().toString(36)

const testOutlet: OutletFormData = {
  name: `E2E Delete Outlet ${uniqueSuffix}`,
  address: `789 Delete Test Blvd, Colombo ${uniqueSuffix}`,
  tel: `0${Date.now().toString().slice(-9)}`,
  nicNo: `NIC_DEL_${uniqueSuffix}`,
  creditLimit: 10000,
  latitude: 6.0535,
  longitude: 80.2210,
  outletType: 'Large',
  outletCategory: 'Wholesale',
}

test.describe.serial('Delete Outlet', () => {
  let outletPage: OutletPage

  test('setup: create an outlet to delete', async ({ page }) => {
    outletPage = new OutletPage(page)
    await outletPage.goto()
    await outletPage.openCreateDialog()

    const dialog = page.locator('[role="dialog"]')

    // Open Route AsyncSelect and type 'a' to bypass the mount-time race condition
    const routeCombobox = dialog.getByRole('combobox').filter({ hasText: 'Select a route' })
    await routeCombobox.scrollIntoViewIfNeeded()
    await routeCombobox.click()
    const routeSearchInput = page.getByPlaceholder('Search route...')
    await routeSearchInput.waitFor({ state: 'visible', timeout: 5_000 })
    await routeSearchInput.fill('a')
    await page.locator('[cmdk-item]:not([data-disabled="true"])').first().waitFor({ state: 'visible', timeout: 8_000 }).catch(() => {})
    const options = page.locator('[cmdk-item]:not([data-disabled="true"])')
    const optionCount = await options.count()

    if (optionCount === 0) {
      await page.keyboard.press('Escape')
      test.skip()
      return
    }

    await options.first().click()
    await routeSearchInput.waitFor({ state: 'hidden', timeout: 3_000 }).catch(() => {})

    await outletPage.fillOutletForm({
      name: testOutlet.name,
      address: testOutlet.address,
      tel: testOutlet.tel,
      nicNo: testOutlet.nicNo,
      creditLimit: testOutlet.creditLimit,
      latitude: testOutlet.latitude,
      longitude: testOutlet.longitude,
      outletType: testOutlet.outletType,
      outletCategory: testOutlet.outletCategory,
    })

    await outletPage.submitCreateForm()
    await outletPage.expectSuccessToast()
    await outletPage.expectDialogClosed()
  })

  test('should show delete confirmation dialog', async ({ page }) => {
    outletPage = new OutletPage(page)
    await outletPage.goto()

    await outletPage.search(testOutlet.name)
    await outletPage.expectRowExists(testOutlet.name)

    await outletPage.clickDelete(testOutlet.name)

    const alertDialog = page.getByRole('alertdialog')
    await expect(alertDialog).toBeVisible()
    await expect(alertDialog.getByText('Delete Outlet')).toBeVisible()
    await expect(
      alertDialog.getByText('This action cannot be undone'),
    ).toBeVisible()
  })

  test('should cancel delete and keep outlet in table', async ({ page }) => {
    outletPage = new OutletPage(page)
    await outletPage.goto()

    await outletPage.search(testOutlet.name)
    await outletPage.expectRowExists(testOutlet.name)

    await outletPage.clickDelete(testOutlet.name)
    await outletPage.cancelAlert()

    // Alert dialog should be gone
    await expect(page.getByRole('alertdialog')).not.toBeAttached({ timeout: 5_000 })

    // Outlet should still exist in the table
    await outletPage.expectRowExists(testOutlet.name)
  })

  test('should delete outlet successfully', async ({ page }) => {
    outletPage = new OutletPage(page)
    await outletPage.goto()

    await outletPage.search(testOutlet.name)
    await outletPage.expectRowExists(testOutlet.name)

    await outletPage.clickDelete(testOutlet.name)
    await outletPage.confirmAlertAction('Delete')

    // Success toast confirms API completed
    await outletPage.expectSuccessToast()

    // Outlet should no longer appear in the table
    await outletPage.search(testOutlet.name)
    const rows = outletPage.table.locator('tbody tr').filter({ hasText: testOutlet.name })
    await expect(rows).toHaveCount(0, { timeout: 10_000 })
  })
})
