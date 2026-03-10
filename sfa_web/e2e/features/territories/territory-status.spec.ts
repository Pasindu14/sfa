import { test, expect } from '@playwright/test'
import { TerritoryPage, type TerritoryFormData } from '../../pages/territory.page'

const uniqueSuffix = Date.now().toString(36) + '2'
const testData: TerritoryFormData = {
  name: `E2E Status Territory ${uniqueSuffix}`,
}

test.describe.serial('Territory Activate / Deactivate', () => {
  let territoryPage: TerritoryPage

  test('setup: create a territory to toggle status', async ({ page }) => {
    territoryPage = new TerritoryPage(page)
    await territoryPage.goto()
    await territoryPage.openCreateDialog()
    await territoryPage.fillTerritoryForm({ name: testData.name })
    await territoryPage.selectFirstArea()
    await territoryPage.submitCreateForm()
    await territoryPage.expectSuccessToast('Territory created successfully')
    await territoryPage.expectDialogClosed()
    await territoryPage.search(testData.name)
    await territoryPage.expectRowExists(testData.name)
    await territoryPage.expectRowStatus(testData.name, 'Active')
  })

  test('should open deactivate confirmation dialog', async ({ page }) => {
    territoryPage = new TerritoryPage(page)
    await territoryPage.goto()
    await territoryPage.search(testData.name)
    await territoryPage.clickDeactivate(testData.name)
    await expect(territoryPage.page.getByRole('alertdialog')).toBeVisible()
    await expect(territoryPage.page.getByRole('heading', { name: 'Deactivate Territory' })).toBeVisible()
  })

  test('should cancel deactivation and keep Active status', async ({ page }) => {
    territoryPage = new TerritoryPage(page)
    await territoryPage.goto()
    await territoryPage.search(testData.name)
    await territoryPage.clickDeactivate(testData.name)
    await territoryPage.cancelAlert()
    await expect(territoryPage.page.getByRole('alertdialog')).not.toBeAttached({ timeout: 10_000 })
    await territoryPage.expectRowStatus(testData.name, 'Active')
  })

  test('should deactivate territory and show Inactive badge', async ({ page }) => {
    territoryPage = new TerritoryPage(page)
    await territoryPage.goto()
    await territoryPage.search(testData.name)
    await territoryPage.clickDeactivate(testData.name)
    await territoryPage.confirmAlertAction('Deactivate')

    await territoryPage.expectSuccessToast('Territory deactivated successfully')
    await territoryPage.expectRowStatus(testData.name, 'Inactive')
  })

  test('should show Activate option in menu after deactivation', async ({ page }) => {
    territoryPage = new TerritoryPage(page)
    await territoryPage.goto()
    await territoryPage.search(testData.name)
    await territoryPage.openRowActions(testData.name)

    await expect(territoryPage.page.getByRole('menuitem', { name: 'Activate', exact: true })).toBeVisible()
    await expect(territoryPage.page.getByRole('menuitem', { name: 'Deactivate', exact: true })).not.toBeVisible()
    // Close the menu
    await territoryPage.page.keyboard.press('Escape')
  })

  test('should open activate confirmation dialog', async ({ page }) => {
    territoryPage = new TerritoryPage(page)
    await territoryPage.goto()
    await territoryPage.search(testData.name)
    await territoryPage.clickActivate(testData.name)
    await expect(territoryPage.page.getByRole('alertdialog')).toBeVisible()
    await expect(territoryPage.page.getByRole('heading', { name: 'Activate Territory' })).toBeVisible()
  })

  test('should activate territory and show Active badge', async ({ page }) => {
    territoryPage = new TerritoryPage(page)
    await territoryPage.goto()
    await territoryPage.search(testData.name)
    await territoryPage.clickActivate(testData.name)
    await territoryPage.confirmAlertAction('Activate')

    await territoryPage.expectSuccessToast('Territory activated successfully')
    await territoryPage.expectRowStatus(testData.name, 'Active')
  })

  test('should show Deactivate option in menu after activation', async ({ page }) => {
    territoryPage = new TerritoryPage(page)
    await territoryPage.goto()
    await territoryPage.search(testData.name)
    await territoryPage.openRowActions(testData.name)

    await expect(territoryPage.page.getByRole('menuitem', { name: 'Deactivate', exact: true })).toBeVisible()
    await expect(territoryPage.page.getByRole('menuitem', { name: 'Activate', exact: true })).not.toBeVisible()
    await territoryPage.page.keyboard.press('Escape')
  })
})
