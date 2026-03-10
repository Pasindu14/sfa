import { test, expect } from '@playwright/test'
import { DivisionPage, type DivisionFormData } from '../../pages/division.page'

const uniqueSuffix = Date.now().toString(36) + '2'
const testData: DivisionFormData = {
  name: `E2E Status Division ${uniqueSuffix}`,
}

test.describe.serial('Division Activate / Deactivate', () => {
  let divisionPage: DivisionPage

  test('setup: create a division to toggle status', async ({ page }) => {
    divisionPage = new DivisionPage(page)
    await divisionPage.goto()
    await divisionPage.openCreateDialog()
    await divisionPage.fillDivisionForm({ name: testData.name })
    await divisionPage.selectFirstTerritory()
    await divisionPage.submitCreateForm()
    await divisionPage.expectSuccessToast('Division created successfully')
    await divisionPage.expectDialogClosed()
    await divisionPage.search(testData.name)
    await divisionPage.expectRowExists(testData.name)
    await divisionPage.expectRowStatus(testData.name, 'Active')
  })

  test('should open deactivate confirmation dialog', async ({ page }) => {
    divisionPage = new DivisionPage(page)
    await divisionPage.goto()
    await divisionPage.search(testData.name)
    await divisionPage.clickDeactivate(testData.name)
    await expect(divisionPage.page.getByRole('alertdialog')).toBeVisible()
    await expect(divisionPage.page.getByRole('heading', { name: 'Deactivate Division' })).toBeVisible()
  })

  test('should cancel deactivation and keep Active status', async ({ page }) => {
    divisionPage = new DivisionPage(page)
    await divisionPage.goto()
    await divisionPage.search(testData.name)
    await divisionPage.clickDeactivate(testData.name)
    await divisionPage.cancelAlert()
    await expect(divisionPage.page.getByRole('alertdialog')).not.toBeAttached({ timeout: 10_000 })
    await divisionPage.expectRowStatus(testData.name, 'Active')
  })

  test('should deactivate division and show Inactive badge', async ({ page }) => {
    divisionPage = new DivisionPage(page)
    await divisionPage.goto()
    await divisionPage.search(testData.name)
    await divisionPage.clickDeactivate(testData.name)
    await divisionPage.confirmAlertAction('Deactivate')

    await divisionPage.expectSuccessToast('Division deactivated successfully')
    await divisionPage.expectRowStatus(testData.name, 'Inactive')
  })

  test('should show Activate option in menu after deactivation', async ({ page }) => {
    divisionPage = new DivisionPage(page)
    await divisionPage.goto()
    await divisionPage.search(testData.name)
    await divisionPage.openRowActions(testData.name)

    await expect(divisionPage.page.getByRole('menuitem', { name: 'Activate', exact: true })).toBeVisible()
    await expect(divisionPage.page.getByRole('menuitem', { name: 'Deactivate', exact: true })).not.toBeVisible()
    // Close the menu
    await divisionPage.page.keyboard.press('Escape')
  })

  test('should open activate confirmation dialog', async ({ page }) => {
    divisionPage = new DivisionPage(page)
    await divisionPage.goto()
    await divisionPage.search(testData.name)
    await divisionPage.clickActivate(testData.name)
    await expect(divisionPage.page.getByRole('alertdialog')).toBeVisible()
    await expect(divisionPage.page.getByRole('heading', { name: 'Activate Division' })).toBeVisible()
  })

  test('should activate division and show Active badge', async ({ page }) => {
    divisionPage = new DivisionPage(page)
    await divisionPage.goto()
    await divisionPage.search(testData.name)
    await divisionPage.clickActivate(testData.name)
    await divisionPage.confirmAlertAction('Activate')

    await divisionPage.expectSuccessToast('Division activated successfully')
    await divisionPage.expectRowStatus(testData.name, 'Active')
  })

  test('should show Deactivate option in menu after activation', async ({ page }) => {
    divisionPage = new DivisionPage(page)
    await divisionPage.goto()
    await divisionPage.search(testData.name)
    await divisionPage.openRowActions(testData.name)

    await expect(divisionPage.page.getByRole('menuitem', { name: 'Deactivate', exact: true })).toBeVisible()
    await expect(divisionPage.page.getByRole('menuitem', { name: 'Activate', exact: true })).not.toBeVisible()
    await divisionPage.page.keyboard.press('Escape')
  })
})
