import { test, expect } from '@playwright/test'
import { AreaPage, type AreaFormData } from '../../pages/area.page'

const uniqueSuffix = Date.now().toString(36) + '1'
const originalData: AreaFormData = {
  name: `E2E Update Area ${uniqueSuffix}`,
}
const updatedData: AreaFormData = {
  name: `E2E Updated Area ${uniqueSuffix}`,
}

test.describe.serial('Area Update', () => {
  let areaPage: AreaPage

  test('setup: create an area to edit', async ({ page }) => {
    areaPage = new AreaPage(page)
    await areaPage.goto()
    await areaPage.openCreateDialog()
    await areaPage.fillAreaForm({ name: originalData.name })
    await areaPage.selectFirstRegion()
    await areaPage.submitCreateForm()
    await areaPage.expectSuccessToast('Area created successfully')
    await areaPage.expectDialogClosed()
    await areaPage.search(originalData.name)
    await areaPage.expectRowExists(originalData.name)
  })

  test('should open edit dialog with pre-filled name', async ({ page }) => {
    areaPage = new AreaPage(page)
    await areaPage.goto()
    await areaPage.search(originalData.name)
    await areaPage.clickEdit(originalData.name)

    await expect(areaPage.page.getByRole('heading', { name: 'Edit Area' })).toBeVisible()
    const dialog = areaPage.page.locator('[role="dialog"]')
    await expect(dialog.getByLabel('Name', { exact: true })).toHaveValue(originalData.name)
  })

  test('should show validation error when name is cleared in edit mode', async ({ page }) => {
    areaPage = new AreaPage(page)
    await areaPage.goto()
    await areaPage.search(originalData.name)
    await areaPage.clickEdit(originalData.name)

    await areaPage.fillAreaForm({ name: '' })
    await areaPage.submitEditForm()
    await areaPage.expectFieldError('Name is required')
    await expect(areaPage.page.getByRole('dialog')).toBeVisible()
  })

  test('should update area name successfully', async ({ page }) => {
    areaPage = new AreaPage(page)
    await areaPage.goto()
    await areaPage.search(originalData.name)
    await areaPage.clickEdit(originalData.name)

    await areaPage.fillAreaForm({ name: updatedData.name })
    await areaPage.submitEditForm()

    await areaPage.expectSuccessToast('Area updated successfully')
    await areaPage.expectDialogClosed()
    await areaPage.search(updatedData.name)
    await areaPage.expectRowExists(updatedData.name)
    await areaPage.expectRowNotExists(originalData.name)
  })
})
