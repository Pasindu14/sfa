import { test, expect } from '@playwright/test'
import { AreaPage, type AreaFormData } from '../../pages/area.page'

const uniqueSuffix = Date.now().toString(36) + '2'
const testData: AreaFormData = {
  name: `E2E Status Area ${uniqueSuffix}`,
}

test.describe.serial('Area Activate / Deactivate', () => {
  let areaPage: AreaPage

  test('setup: create an area to toggle status', async ({ page }) => {
    areaPage = new AreaPage(page)
    await areaPage.goto()
    await areaPage.openCreateDialog()
    await areaPage.fillAreaForm({ name: testData.name })
    await areaPage.selectFirstRegion()
    await areaPage.submitCreateForm()
    await areaPage.expectSuccessToast('Area created successfully')
    await areaPage.expectDialogClosed()
    await areaPage.expectRowExists(testData.name)
    await areaPage.expectRowStatus(testData.name, 'Active')
  })

  test('should open deactivate confirmation dialog', async ({ page }) => {
    areaPage = new AreaPage(page)
    await areaPage.goto()
    await areaPage.clickDeactivate(testData.name)
    await expect(areaPage.page.getByRole('alertdialog')).toBeVisible()
    await expect(areaPage.page.getByRole('heading', { name: 'Deactivate Area' })).toBeVisible()
  })

  test('should cancel deactivation and keep Active status', async ({ page }) => {
    areaPage = new AreaPage(page)
    await areaPage.goto()
    await areaPage.clickDeactivate(testData.name)
    await areaPage.cancelAlert()
    await expect(areaPage.page.getByRole('alertdialog')).not.toBeAttached({ timeout: 10_000 })
    await areaPage.expectRowStatus(testData.name, 'Active')
  })

  test('should deactivate area and show Inactive badge', async ({ page }) => {
    areaPage = new AreaPage(page)
    await areaPage.goto()
    await areaPage.clickDeactivate(testData.name)
    await areaPage.confirmAlertAction('Deactivate')

    await areaPage.expectSuccessToast('Area deactivated successfully')
    await areaPage.expectRowStatus(testData.name, 'Inactive')
  })

  test('should show Activate option in menu after deactivation', async ({ page }) => {
    areaPage = new AreaPage(page)
    await areaPage.goto()
    await areaPage.openRowActions(testData.name)

    await expect(areaPage.page.getByRole('menuitem', { name: 'Activate', exact: true })).toBeVisible()
    await expect(areaPage.page.getByRole('menuitem', { name: 'Deactivate', exact: true })).not.toBeVisible()
    // Close the menu
    await areaPage.page.keyboard.press('Escape')
  })

  test('should open activate confirmation dialog', async ({ page }) => {
    areaPage = new AreaPage(page)
    await areaPage.goto()
    await areaPage.clickActivate(testData.name)
    await expect(areaPage.page.getByRole('alertdialog')).toBeVisible()
    await expect(areaPage.page.getByRole('heading', { name: 'Activate Area' })).toBeVisible()
  })

  test('should activate area and show Active badge', async ({ page }) => {
    areaPage = new AreaPage(page)
    await areaPage.goto()
    await areaPage.clickActivate(testData.name)
    await areaPage.confirmAlertAction('Activate')

    await areaPage.expectSuccessToast('Area activated successfully')
    await areaPage.expectRowStatus(testData.name, 'Active')
  })

  test('should show Deactivate option in menu after activation', async ({ page }) => {
    areaPage = new AreaPage(page)
    await areaPage.goto()
    await areaPage.openRowActions(testData.name)

    await expect(areaPage.page.getByRole('menuitem', { name: 'Deactivate', exact: true })).toBeVisible()
    await expect(areaPage.page.getByRole('menuitem', { name: 'Activate', exact: true })).not.toBeVisible()
    await areaPage.page.keyboard.press('Escape')
  })
})
