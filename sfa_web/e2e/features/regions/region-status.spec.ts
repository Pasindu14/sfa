import { test, expect } from '@playwright/test'
import { RegionPage, type RegionFormData } from '../../pages/region.page'

const uniqueSuffix = Date.now().toString(36) + '2'
const testData: RegionFormData = {
  name: `E2E Status Region ${uniqueSuffix}`,
}

test.describe.serial('Region Activate / Deactivate', () => {
  let regionPage: RegionPage

  test('setup: create a region to toggle status', async ({ page }) => {
    regionPage = new RegionPage(page)
    await regionPage.goto()
    await regionPage.openCreateDialog()
    await regionPage.fillRegionForm(testData)
    await regionPage.submitCreateForm()
    await regionPage.expectSuccessToast('Region created successfully')
    await regionPage.expectDialogClosed()
    await regionPage.expectRowExists(testData.name)
    await regionPage.expectRowStatus(testData.name, 'Active')
  })

  test('should open deactivate confirmation dialog', async ({ page }) => {
    regionPage = new RegionPage(page)
    await regionPage.goto()
    await regionPage.clickDeactivate(testData.name)
    await expect(regionPage.page.getByRole('alertdialog')).toBeVisible()
    await expect(regionPage.page.getByRole('heading', { name: 'Deactivate Region' })).toBeVisible()
  })

  test('should cancel deactivation and keep Active status', async ({ page }) => {
    regionPage = new RegionPage(page)
    await regionPage.goto()
    await regionPage.clickDeactivate(testData.name)
    await regionPage.cancelAlert()
    await expect(regionPage.page.getByRole('alertdialog')).not.toBeAttached({ timeout: 10_000 })
    await regionPage.expectRowStatus(testData.name, 'Active')
  })

  test('should deactivate region and show Inactive badge', async ({ page }) => {
    regionPage = new RegionPage(page)
    await regionPage.goto()
    await regionPage.clickDeactivate(testData.name)
    await regionPage.confirmAlertAction('Deactivate')

    await regionPage.expectSuccessToast('Region deactivated successfully')
    await regionPage.expectRowStatus(testData.name, 'Inactive')
  })

  test('should show Activate option in menu after deactivation', async ({ page }) => {
    regionPage = new RegionPage(page)
    await regionPage.goto()
    await regionPage.openRowActions(testData.name)

    await expect(regionPage.page.getByRole('menuitem', { name: 'Activate' })).toBeVisible()
    await expect(regionPage.page.getByRole('menuitem', { name: 'Deactivate' })).not.toBeVisible()
    // Close the menu
    await regionPage.page.keyboard.press('Escape')
  })

  test('should open activate confirmation dialog', async ({ page }) => {
    regionPage = new RegionPage(page)
    await regionPage.goto()
    await regionPage.clickActivate(testData.name)
    await expect(regionPage.page.getByRole('alertdialog')).toBeVisible()
    await expect(regionPage.page.getByRole('heading', { name: 'Activate Region' })).toBeVisible()
  })

  test('should activate region and show Active badge', async ({ page }) => {
    regionPage = new RegionPage(page)
    await regionPage.goto()
    await regionPage.clickActivate(testData.name)
    await regionPage.confirmAlertAction('Activate')

    await regionPage.expectSuccessToast('Region activated successfully')
    await regionPage.expectRowStatus(testData.name, 'Active')
  })

  test('should show Deactivate option in menu after activation', async ({ page }) => {
    regionPage = new RegionPage(page)
    await regionPage.goto()
    await regionPage.openRowActions(testData.name)

    await expect(regionPage.page.getByRole('menuitem', { name: 'Deactivate' })).toBeVisible()
    await expect(regionPage.page.getByRole('menuitem', { name: 'Activate' })).not.toBeVisible()
    await regionPage.page.keyboard.press('Escape')
  })
})
