import { test, expect } from '@playwright/test'
import { OutletPage, type OutletFormData } from '../../pages/outlet.page'

const uniqueSuffix = Date.now().toString(36)

const testOutlet: OutletFormData = {
  name: `E2E Edit Outlet ${uniqueSuffix}`,
  address: `456 Edit Test Ave, Colombo ${uniqueSuffix}`,
  tel: `0${Date.now().toString().slice(-9)}`,
  nicNo: `NIC_EDIT_${uniqueSuffix}`,
  creditLimit: 30000,
  latitude: 7.2906,
  longitude: 80.6337,
  outletType: 'Medium',
  outletCategory: 'SMMT',
}

test.describe.serial('Update Outlet', () => {
  let outletPage: OutletPage

  test('setup: create an outlet to edit', async ({ page }) => {
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

  test('should open edit dialog with pre-filled data', async ({ page }) => {
    outletPage = new OutletPage(page)
    await outletPage.goto()

    await outletPage.search(testOutlet.name)
    await outletPage.expectRowExists(testOutlet.name)

    await outletPage.clickEdit(testOutlet.name)

    const dialog = page.locator('[role="dialog"]')
    await expect(dialog.getByRole('heading', { name: 'Edit Outlet' })).toBeVisible()

    // Name field should be pre-filled
    const nameInput = dialog.getByLabel('Name', { exact: true })
    await expect(nameInput).toHaveValue(testOutlet.name)

    // Address field should be pre-filled
    const addressInput = dialog.getByLabel('Address', { exact: true })
    await expect(addressInput).toHaveValue(testOutlet.address)

    // NIC field should be pre-filled
    const nicInput = dialog.getByLabel('NIC Number', { exact: true })
    await expect(nicInput).toHaveValue(testOutlet.nicNo)
  })

  test('should update outlet name successfully', async ({ page }) => {
    outletPage = new OutletPage(page)
    await outletPage.goto()

    await outletPage.search(testOutlet.name)
    await outletPage.expectRowExists(testOutlet.name)

    await outletPage.clickEdit(testOutlet.name)

    const updatedName = `Updated ${testOutlet.name}`
    await outletPage.fillOutletForm({ name: updatedName })
    await outletPage.submitEditForm()

    // Success toast confirms API completed
    await outletPage.expectSuccessToast()

    // Dialog should close after mutation success
    await outletPage.expectDialogClosed()

    // Table should reflect the updated name
    await outletPage.search(updatedName)
    await outletPage.expectRowExists(updatedName)
  })

  test('should show validation error when clearing required name field', async ({ page }) => {
    outletPage = new OutletPage(page)
    await outletPage.goto()

    const updatedName = `Updated ${testOutlet.name}`
    await outletPage.search(updatedName)

    await outletPage.clickEdit(updatedName)

    await outletPage.fillOutletForm({ name: '' })
    await outletPage.submitEditForm()

    const dialog = page.locator('[role="dialog"]')
    await expect(dialog).toBeVisible()
    await outletPage.expectFieldError('Name is required')
  })

  test('should show validation error when clearing required address field', async ({ page }) => {
    outletPage = new OutletPage(page)
    await outletPage.goto()

    const updatedName = `Updated ${testOutlet.name}`
    await outletPage.search(updatedName)

    await outletPage.clickEdit(updatedName)

    await outletPage.fillOutletForm({ address: '' })
    await outletPage.submitEditForm()

    const dialog = page.locator('[role="dialog"]')
    await expect(dialog).toBeVisible()
    await outletPage.expectFieldError('Address is required')
  })

  test('should show validation error when clearing required NIC field', async ({ page }) => {
    outletPage = new OutletPage(page)
    await outletPage.goto()

    const updatedName = `Updated ${testOutlet.name}`
    await outletPage.search(updatedName)

    await outletPage.clickEdit(updatedName)

    await outletPage.fillOutletForm({ nicNo: '' })
    await outletPage.submitEditForm()

    const dialog = page.locator('[role="dialog"]')
    await expect(dialog).toBeVisible()
    await outletPage.expectFieldError('NIC number is required')
  })
})
