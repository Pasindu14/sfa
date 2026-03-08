import { test, expect } from '@playwright/test'
import { DistributorPage, type DistributorFormData } from '../../pages/distributor.page'

const uniqueSuffix = Date.now().toString(36)
const uniquePhone = `+1${Date.now().toString().slice(-9)}`

const testDistributor: DistributorFormData = {
  name: `E2E Edit Dist ${uniqueSuffix}`,
  alias: Math.floor(Math.random() * 90000) + 10000,
  address: `456 Edit Test Ave, Edit City ${uniqueSuffix}`,
  phone: uniquePhone,
  email: `e2e_edit_dist_${uniqueSuffix}@test.com`,
  tradeDiscount: 8,
  commission: 4,
}

test.describe.serial('Update Distributor', () => {
  let distributorPage: DistributorPage

  test('setup: create a distributor to edit', async ({ page }) => {
    distributorPage = new DistributorPage(page)
    await distributorPage.goto()

    await distributorPage.openCreateDialog()
    await distributorPage.fillDistributorForm(testDistributor)
    await distributorPage.submitCreateForm()

    await distributorPage.expectSuccessToast()
    await distributorPage.expectDialogClosed()
  })

  test('should open edit dialog with pre-filled data', async ({ page }) => {
    distributorPage = new DistributorPage(page)
    await distributorPage.goto()

    await distributorPage.search(testDistributor.name)
    await distributorPage.expectRowExists(testDistributor.name)

    await distributorPage.clickEdit(testDistributor.name)

    // Edit dialog should be visible with the correct heading
    const dialog = page.locator('[role="dialog"]')
    await expect(dialog.getByRole('heading', { name: 'Edit Distributor' })).toBeVisible()

    // Name field should be pre-filled with the existing value
    const nameInput = dialog.getByLabel('Name', { exact: true })
    await expect(nameInput).toHaveValue(testDistributor.name)
  })

  test('should update distributor name successfully', async ({ page }) => {
    distributorPage = new DistributorPage(page)
    await distributorPage.goto()

    await distributorPage.search(testDistributor.name)
    await distributorPage.expectRowExists(testDistributor.name)

    await distributorPage.clickEdit(testDistributor.name)

    const updatedName = `Updated ${testDistributor.name}`
    await distributorPage.fillDistributorForm({ name: updatedName })
    await distributorPage.submitEditForm()

    // Success toast confirms API completed
    await distributorPage.expectSuccessToast()

    // Dialog should close after mutation success
    await distributorPage.expectDialogClosed()

    // Table should reflect the updated name
    await distributorPage.search(updatedName)
    await distributorPage.expectRowExists(updatedName)
  })

  test('should show validation error when clearing required name field', async ({ page }) => {
    distributorPage = new DistributorPage(page)
    await distributorPage.goto()

    // Search with updated name from prior step
    const updatedName = `Updated ${testDistributor.name}`
    await distributorPage.search(updatedName)

    await distributorPage.clickEdit(updatedName)

    // Clear the name field and submit
    await distributorPage.fillDistributorForm({ name: '' })
    await distributorPage.submitEditForm()

    // Dialog should remain open (validation error)
    const dialog = page.locator('[role="dialog"]')
    await expect(dialog).toBeVisible()
  })

  test('should show validation error when clearing required address field', async ({ page }) => {
    distributorPage = new DistributorPage(page)
    await distributorPage.goto()

    const updatedName = `Updated ${testDistributor.name}`
    await distributorPage.search(updatedName)

    await distributorPage.clickEdit(updatedName)

    await distributorPage.fillDistributorForm({ address: '' })
    await distributorPage.submitEditForm()

    const dialog = page.locator('[role="dialog"]')
    await expect(dialog).toBeVisible()
  })
})
