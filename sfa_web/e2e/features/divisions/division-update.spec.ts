import { test, expect } from '@playwright/test'
import { DivisionPage, type DivisionFormData } from '../../pages/division.page'

const uniqueSuffix = Date.now().toString(36) + '1'
const originalData: DivisionFormData = {
  name: `E2E Update Division ${uniqueSuffix}`,
}
const updatedData: DivisionFormData = {
  name: `E2E Updated Division ${uniqueSuffix}`,
}

test.describe.serial('Division Update', () => {
  let divisionPage: DivisionPage

  test('setup: create a division to edit', async ({ page }) => {
    divisionPage = new DivisionPage(page)
    await divisionPage.goto()
    await divisionPage.openCreateDialog()
    await divisionPage.fillDivisionForm({ name: originalData.name })
    await divisionPage.selectFirstTerritory()
    await divisionPage.submitCreateForm()
    await divisionPage.expectSuccessToast('Division created successfully')
    await divisionPage.expectDialogClosed()
    await divisionPage.search(originalData.name)
    await divisionPage.expectRowExists(originalData.name)
  })

  test('should open edit dialog with pre-filled name', async ({ page }) => {
    divisionPage = new DivisionPage(page)
    await divisionPage.goto()
    await divisionPage.search(originalData.name)
    await divisionPage.clickEdit(originalData.name)

    await expect(divisionPage.page.getByRole('heading', { name: 'Edit Division' })).toBeVisible()
    const dialog = divisionPage.page.locator('[role="dialog"]')
    await expect(dialog.getByLabel('Name', { exact: true })).toHaveValue(originalData.name)
  })

  test('should show validation error when name is cleared in edit mode', async ({ page }) => {
    divisionPage = new DivisionPage(page)
    await divisionPage.goto()
    await divisionPage.search(originalData.name)
    await divisionPage.clickEdit(originalData.name)

    await divisionPage.fillDivisionForm({ name: '' })
    await divisionPage.submitEditForm()
    await divisionPage.expectFieldError('Name is required')
    await expect(divisionPage.page.getByRole('dialog')).toBeVisible()
  })

  test('should update division name successfully', async ({ page }) => {
    divisionPage = new DivisionPage(page)
    await divisionPage.goto()
    await divisionPage.search(originalData.name)
    await divisionPage.clickEdit(originalData.name)

    await divisionPage.fillDivisionForm({ name: updatedData.name })
    await divisionPage.submitEditForm()

    await divisionPage.expectSuccessToast('Division updated successfully')
    await divisionPage.expectDialogClosed()
    await divisionPage.search(updatedData.name)
    await divisionPage.expectRowExists(updatedData.name)
    await divisionPage.expectRowNotExists(originalData.name)
  })
})
