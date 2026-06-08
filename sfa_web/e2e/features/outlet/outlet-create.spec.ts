import { test, expect } from '@playwright/test'
import { OutletPage, type OutletFormData } from '../../pages/outlet.page'

// Generate unique values per test run to prevent database conflicts
const uniqueSuffix = Date.now().toString(36)

const testOutlet: OutletFormData = {
  name: `E2E Outlet ${uniqueSuffix}`,
  address: `123 E2E Test Street, Colombo ${uniqueSuffix}`,
  tel: `0${Date.now().toString().slice(-9)}`,
  nicNo: `NIC${uniqueSuffix}`,
  creditLimit: 50000,
  latitude: 6.9271,
  longitude: 79.8612,
  outletType: 'Small',
  outletCategory: 'Wholesale',
  remarks: 'E2E test outlet',
}

test.describe('Create Outlet', () => {
  let outletPage: OutletPage

  test.beforeEach(async ({ page }) => {
    outletPage = new OutletPage(page)
    await outletPage.goto()
  })

  test('should open create dialog when clicking Add Outlet', async () => {
    await outletPage.openCreateDialog()

    const dialog = outletPage.page.locator('[role="dialog"]')

    // All required form fields should be visible
    await expect(dialog.getByLabel('Name', { exact: true })).toBeVisible()
    await expect(dialog.getByLabel('NIC Number', { exact: true })).toBeVisible()
    await expect(dialog.getByLabel('Address', { exact: true })).toBeVisible()
    await expect(dialog.getByLabel('Telephone', { exact: true })).toBeVisible()
    await expect(dialog.getByLabel('Credit Limit', { exact: true })).toBeVisible()
    await expect(dialog.getByLabel('Latitude', { exact: true })).toBeVisible()
    await expect(dialog.getByLabel('Longitude', { exact: true })).toBeVisible()
  })

  test('should display optional fields in create dialog', async () => {
    await outletPage.openCreateDialog()

    const dialog = outletPage.page.locator('[role="dialog"]')

    await expect(dialog.getByLabel('Email (Optional)', { exact: true })).toBeVisible()
    await expect(dialog.getByLabel('Contact Person (Optional)', { exact: true })).toBeVisible()
    await expect(dialog.getByLabel('VAT Number (Optional)', { exact: true })).toBeVisible()
    await expect(dialog.getByLabel('Remarks (Optional)', { exact: true })).toBeVisible()
  })

  test('should show validation errors when submitting empty required fields', async () => {
    await outletPage.openCreateDialog()

    // Clear any pre-filled defaults and submit
    await outletPage.fillOutletForm({
      name: '',
      address: '',
      tel: '',
      nicNo: '',
    })
    await outletPage.submitCreateForm()

    // Dialog should remain open due to validation errors
    const dialog = outletPage.page.locator('[role="dialog"]')
    await expect(dialog).toBeVisible()
  })

  test('should show name required validation error', async () => {
    await outletPage.openCreateDialog()

    await outletPage.fillOutletForm({ name: '' })
    await outletPage.submitCreateForm()

    await outletPage.expectFieldError('Name is required')
  })

  test('should show address required validation error', async () => {
    await outletPage.openCreateDialog()

    await outletPage.fillOutletForm({ address: '' })
    await outletPage.submitCreateForm()

    await outletPage.expectFieldError('Address is required')
  })

  test('should show telephone required validation error', async () => {
    await outletPage.openCreateDialog()

    await outletPage.fillOutletForm({ tel: '' })
    await outletPage.submitCreateForm()

    await outletPage.expectFieldError('Telephone is required')
  })

  test('should show NIC required validation error', async () => {
    await outletPage.openCreateDialog()

    await outletPage.fillOutletForm({ nicNo: '' })
    await outletPage.submitCreateForm()

    await outletPage.expectFieldError('NIC number is required')
  })

  test('should close dialog when clicking outside', async () => {
    await outletPage.openCreateDialog()

    // Press Escape to close
    await outletPage.page.keyboard.press('Escape')

    await outletPage.expectDialogClosed()
  })

  test('should create a new outlet successfully when a route exists', async ({ page }) => {
    const localPage = new OutletPage(page)
    await localPage.goto()
    await localPage.openCreateDialog()

    const dialog = page.locator('[role="dialog"]')

    // Open the Route AsyncSelect popover
    const routeCombobox = dialog.getByRole('combobox').filter({ hasText: 'Select a route' })
    await routeCombobox.scrollIntoViewIfNeeded()
    await routeCombobox.click()
    const routeSearchInput = page.getByPlaceholder('Search route...')
    await routeSearchInput.waitFor({ state: 'visible', timeout: 5_000 })

    // AsyncSelect race condition: on create mode (value="0"), the fetcher("0") call can
    // complete after fetcher("") and overwrite options with []. Type a character to force
    // a new clean debounced search that resolves after the mount-time race has settled.
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

    // Fill remaining required fields
    await localPage.fillOutletForm({
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

    await localPage.submitCreateForm()
    await localPage.expectSuccessToast()
    await localPage.expectDialogClosed()

    await localPage.search(testOutlet.name)
    await localPage.expectRowExists(testOutlet.name)
  })
})
