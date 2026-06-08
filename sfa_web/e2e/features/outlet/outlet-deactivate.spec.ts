import { test, expect } from '@playwright/test'
import { OutletPage, type OutletFormData } from '../../pages/outlet.page'

const uniqueSuffix = Date.now().toString(36)

const testOutlet: OutletFormData = {
  name: `E2E Status Outlet ${uniqueSuffix}`,
  address: `321 Status Test Road, Kandy ${uniqueSuffix}`,
  tel: `0${Date.now().toString().slice(-9)}`,
  nicNo: `NIC_STAT_${uniqueSuffix}`,
  creditLimit: 25000,
  latitude: 7.2906,
  longitude: 80.6337,
  outletType: 'Small',
  outletCategory: 'SMMT',
}

test.describe.serial('Deactivate & Activate Outlet', () => {
  let outletPage: OutletPage

  test('setup: create an outlet', async ({ page }) => {
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

  test('should show deactivate confirmation dialog', async ({ page }) => {
    outletPage = new OutletPage(page)
    await outletPage.goto()

    await outletPage.search(testOutlet.name)
    await outletPage.expectRowExists(testOutlet.name)

    await outletPage.clickDeactivate(testOutlet.name)

    const alertDialog = page.getByRole('alertdialog')
    await expect(alertDialog).toBeVisible()
    await expect(alertDialog.getByText('Deactivate Outlet')).toBeVisible()
  })

  test('should cancel deactivate and keep outlet active', async ({ page }) => {
    outletPage = new OutletPage(page)
    await outletPage.goto()

    await outletPage.search(testOutlet.name)
    await outletPage.expectRowExists(testOutlet.name)

    await outletPage.clickDeactivate(testOutlet.name)
    await outletPage.cancelAlert()

    // Outlet should still be Active
    await outletPage.expectRowStatus(testOutlet.name, 'Active')
  })

  test('should deactivate outlet successfully', async ({ page }) => {
    outletPage = new OutletPage(page)
    await outletPage.goto()

    await outletPage.search(testOutlet.name)
    await outletPage.expectRowExists(testOutlet.name)
    await outletPage.expectRowStatus(testOutlet.name, 'Active')

    await outletPage.clickDeactivate(testOutlet.name)
    await outletPage.confirmAlertAction('Deactivate')

    await outletPage.expectSuccessToast()

    // Outlet should now show Inactive status
    await outletPage.search(testOutlet.name)
    await outletPage.expectRowExists(testOutlet.name)
    await outletPage.expectRowStatus(testOutlet.name, 'Inactive')
  })

  test('should show Activate option for inactive outlet', async ({ page }) => {
    outletPage = new OutletPage(page)
    await outletPage.goto()

    await outletPage.search(testOutlet.name)
    await outletPage.expectRowExists(testOutlet.name)
    await outletPage.expectRowStatus(testOutlet.name, 'Inactive')

    // Dropdown should now show "Activate" instead of "Deactivate"
    await outletPage.openRowActions(testOutlet.name)
    await expect(page.getByRole('menuitem', { name: 'Activate' })).toBeVisible()
  })

  test('should activate outlet successfully', async ({ page }) => {
    outletPage = new OutletPage(page)
    await outletPage.goto()

    await outletPage.search(testOutlet.name)
    await outletPage.expectRowExists(testOutlet.name)

    await outletPage.clickActivate(testOutlet.name)
    await outletPage.confirmAlertAction('Activate')

    await outletPage.expectSuccessToast()

    // Outlet should now show Active status again
    await outletPage.search(testOutlet.name)
    await outletPage.expectRowExists(testOutlet.name)
    await outletPage.expectRowStatus(testOutlet.name, 'Active')
  })
})
