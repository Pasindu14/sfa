import { test, expect } from '@playwright/test'
import { DistributorPage, type DistributorFormData } from '../../pages/distributor.page'

// Generate unique values per test run to prevent database conflicts
const uniqueSuffix = Date.now().toString(36)
const uniquePhone = `+3${Date.now().toString().slice(-9)}`

const testDistributor: DistributorFormData = {
  name: `E2E Distributor ${uniqueSuffix}`,
  alias: Math.floor(Math.random() * 90000) + 10000, // 5-digit random alias
  address: `123 E2E Test Street, Test City ${uniqueSuffix}`,
  phone: uniquePhone,
  email: `e2e_dist_${uniqueSuffix}@test.com`,
  tradeDiscount: 10,
  commission: 5,
  vatRegNo: `VAT${uniqueSuffix}`,
  remark: 'E2E test distributor',
}

test.describe('Create Distributor', () => {
  let distributorPage: DistributorPage

  test.beforeEach(async ({ page }) => {
    distributorPage = new DistributorPage(page)
    await distributorPage.goto()
  })

  test('should open create dialog when clicking Add Distributor', async () => {
    await distributorPage.openCreateDialog()

    // Dialog should contain all required form fields
    const dialog = distributorPage.page.locator('[role="dialog"]')
    await expect(dialog.getByLabel('Name', { exact: true })).toBeVisible()
    await expect(dialog.getByLabel('Alias (Numbers only)', { exact: true })).toBeVisible()
    await expect(dialog.getByLabel('Address', { exact: true })).toBeVisible()
    await expect(dialog.getByLabel('Phone', { exact: true })).toBeVisible()
    await expect(dialog.getByLabel('Email', { exact: true })).toBeVisible()
    await expect(dialog.getByLabel('Trade Discount (%) *', { exact: true })).toBeVisible()
    await expect(dialog.getByLabel('Commission (%) *', { exact: true })).toBeVisible()
  })

  test('should show validation errors on empty submit', async () => {
    await distributorPage.openCreateDialog()

    // Clear required fields and submit
    await distributorPage.fillDistributorForm({
      name: '',
      address: '',
      phone: '',
      email: '',
    })
    await distributorPage.submitCreateForm()

    // Dialog should remain open due to validation errors
    const dialog = distributorPage.page.locator('[role="dialog"]')
    await expect(dialog).toBeVisible()
  })

  test('should show validation error for invalid email', async () => {
    await distributorPage.openCreateDialog()

    await distributorPage.fillDistributorForm({
      name: testDistributor.name,
      alias: testDistributor.alias,
      address: testDistributor.address,
      phone: testDistributor.phone,
      email: 'not-a-valid-email',
      tradeDiscount: testDistributor.tradeDiscount,
      commission: testDistributor.commission,
    })
    await distributorPage.submitCreateForm()

    await distributorPage.expectFieldError('Invalid email format')
  })

  test('should show validation error for invalid phone', async () => {
    await distributorPage.openCreateDialog()

    await distributorPage.fillDistributorForm({
      name: testDistributor.name,
      alias: testDistributor.alias,
      address: testDistributor.address,
      phone: 'abc', // too short and non-numeric
      email: testDistributor.email,
      tradeDiscount: testDistributor.tradeDiscount,
      commission: testDistributor.commission,
    })
    await distributorPage.submitCreateForm()

    // Zod: min(10) or invalid phone format
    const dialog = distributorPage.page.locator('[role="dialog"]')
    await expect(dialog).toBeVisible()
  })

  test('should create a new distributor successfully', async () => {
    await distributorPage.openCreateDialog()
    await distributorPage.fillDistributorForm(testDistributor)
    await distributorPage.submitCreateForm()

    // Success toast confirms API completed
    await distributorPage.expectSuccessToast()

    // Dialog should close after mutation success
    await distributorPage.expectDialogClosed()

    // New distributor should appear in the table
    await distributorPage.search(testDistributor.name)
    await distributorPage.expectRowExists(testDistributor.name)
  })

  test('should keep dialog open when submitting duplicate distributor', async () => {
    // Attempt to create the same distributor again (duplicate name/alias/email)
    await distributorPage.openCreateDialog()
    await distributorPage.fillDistributorForm(testDistributor)
    await distributorPage.submitCreateForm()

    // API should return a conflict error — dialog stays open
    const dialog = distributorPage.page.locator('[role="dialog"]')
    await expect(dialog).toBeVisible({ timeout: 5_000 })
  })
})
