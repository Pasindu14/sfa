import { test, expect } from '@playwright/test'
import { DistributorPage, type DistributorFormData } from '../../pages/distributor.page'

const uniqueSuffix = Date.now().toString(36)
const uniquePhone = `+4${Date.now().toString().slice(-9)}`

const testDistributor: DistributorFormData = {
  name: `E2E Delete Dist ${uniqueSuffix}`,
  alias: Math.floor(Math.random() * 90000) + 10000,
  address: `789 Delete Test Blvd, Delete City ${uniqueSuffix}`,
  phone: uniquePhone,
  email: `e2e_delete_dist_${uniqueSuffix}@test.com`,
  tradeDiscount: 5,
  commission: 2,
}

test.describe.serial('Delete Distributor', () => {
  let distributorPage: DistributorPage

  test('setup: create a distributor to delete', async ({ page }) => {
    distributorPage = new DistributorPage(page)
    await distributorPage.goto()

    await distributorPage.openCreateDialog()
    await distributorPage.fillDistributorForm(testDistributor)
    await distributorPage.submitCreateForm()

    await distributorPage.expectSuccessToast()
    await distributorPage.expectDialogClosed()
  })

  test('should show delete confirmation dialog', async ({ page }) => {
    distributorPage = new DistributorPage(page)
    await distributorPage.goto()

    await distributorPage.search(testDistributor.name)
    await distributorPage.expectRowExists(testDistributor.name)

    await distributorPage.clickDelete(testDistributor.name)

    const alertDialog = page.getByRole('alertdialog')
    await expect(alertDialog).toBeVisible()
    await expect(alertDialog.getByText('Delete Distributor')).toBeVisible()
    await expect(
      alertDialog.getByText('This action cannot be undone.')
    ).toBeVisible()
  })

  test('should cancel delete and keep distributor in table', async ({ page }) => {
    distributorPage = new DistributorPage(page)
    await distributorPage.goto()

    await distributorPage.search(testDistributor.name)
    await distributorPage.expectRowExists(testDistributor.name)

    await distributorPage.clickDelete(testDistributor.name)
    await distributorPage.cancelAlert()

    // Distributor should still be in the table
    await distributorPage.expectRowExists(testDistributor.name)
  })

  test('should delete distributor successfully', async ({ page }) => {
    distributorPage = new DistributorPage(page)
    await distributorPage.goto()

    await distributorPage.search(testDistributor.name)
    await distributorPage.expectRowExists(testDistributor.name)

    await distributorPage.clickDelete(testDistributor.name)
    await distributorPage.confirmAlertAction('Delete')

    // Success toast confirms API completed
    await distributorPage.expectSuccessToast()

    // Distributor should no longer be in the table
    await distributorPage.search(testDistributor.name)
    await distributorPage.expectRowNotExists(testDistributor.name)
  })
})
