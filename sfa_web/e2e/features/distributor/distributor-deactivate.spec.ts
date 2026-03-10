import { test, expect } from '@playwright/test'
import { DistributorPage, type DistributorFormData } from '../../pages/distributor.page'

const uniqueSuffix = Date.now().toString(36)
const uniquePhone = `+2${Date.now().toString().slice(-9)}`

const testDistributor: DistributorFormData = {
  name: `E2E Status Dist ${uniqueSuffix}`,
  alias: Math.floor(Math.random() * 90000) + 10000,
  address: `321 Status Test Road, Status City ${uniqueSuffix}`,
  phone: uniquePhone,
  email: `e2e_status_dist_${uniqueSuffix}@test.com`,
  tradeDiscount: 12,
  commission: 6,
}

test.describe.serial('Deactivate & Activate Distributor', () => {
  let distributorPage: DistributorPage

  test('setup: create a distributor', async ({ page }) => {
    distributorPage = new DistributorPage(page)
    await distributorPage.goto()

    await distributorPage.openCreateDialog()
    await distributorPage.fillDistributorForm(testDistributor)
    await distributorPage.submitCreateForm()

    await distributorPage.expectSuccessToast()
    await distributorPage.expectDialogClosed()
  })

  test('should show deactivate confirmation dialog', async ({ page }) => {
    distributorPage = new DistributorPage(page)
    await distributorPage.goto()

    await distributorPage.search(testDistributor.name)
    await distributorPage.expectRowExists(testDistributor.name)

    await distributorPage.clickDeactivate(testDistributor.name)

    const alertDialog = page.getByRole('alertdialog')
    await expect(alertDialog).toBeVisible()
    await expect(alertDialog.getByText('Deactivate Distributor')).toBeVisible()
  })

  test('should cancel deactivate and keep distributor active', async ({ page }) => {
    distributorPage = new DistributorPage(page)
    await distributorPage.goto()

    await distributorPage.search(testDistributor.name)
    await distributorPage.expectRowExists(testDistributor.name)

    await distributorPage.clickDeactivate(testDistributor.name)
    await distributorPage.cancelAlert()

    // Distributor should still be Active
    await distributorPage.expectRowStatus(testDistributor.name, 'Active')
  })

  test('should deactivate distributor successfully', async ({ page }) => {
    distributorPage = new DistributorPage(page)
    await distributorPage.goto()

    await distributorPage.search(testDistributor.name)
    await distributorPage.expectRowExists(testDistributor.name)
    await distributorPage.expectRowStatus(testDistributor.name, 'Active')

    await distributorPage.clickDeactivate(testDistributor.name)
    await distributorPage.confirmAlertAction('Deactivate')

    await distributorPage.expectSuccessToast()

    // Distributor should now show Inactive status
    await distributorPage.search(testDistributor.name)
    await distributorPage.expectRowExists(testDistributor.name)
    await distributorPage.expectRowStatus(testDistributor.name, 'Inactive')
  })

  test('should show activate option for inactive distributor', async ({ page }) => {
    distributorPage = new DistributorPage(page)
    await distributorPage.goto()

    await distributorPage.search(testDistributor.name)
    await distributorPage.expectRowExists(testDistributor.name)
    await distributorPage.expectRowStatus(testDistributor.name, 'Inactive')

    // Dropdown should now show "Activate" instead of "Deactivate"
    await distributorPage.openRowActions(testDistributor.name)
    await expect(page.getByRole('menuitem', { name: 'Activate' })).toBeVisible()
  })

  test('should activate distributor successfully', async ({ page }) => {
    distributorPage = new DistributorPage(page)
    await distributorPage.goto()

    await distributorPage.search(testDistributor.name)
    await distributorPage.expectRowExists(testDistributor.name)

    await distributorPage.clickActivate(testDistributor.name)
    await distributorPage.confirmAlertAction('Activate')

    await distributorPage.expectSuccessToast()

    // Distributor should now show Active status again
    await distributorPage.search(testDistributor.name)
    await distributorPage.expectRowExists(testDistributor.name)
    await distributorPage.expectRowStatus(testDistributor.name, 'Active')
  })
})
