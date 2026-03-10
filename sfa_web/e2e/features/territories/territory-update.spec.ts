import { test, expect } from '@playwright/test'
import { TerritoryPage, type TerritoryFormData } from '../../pages/territory.page'

const uniqueSuffix = Date.now().toString(36) + '1'
const originalData: TerritoryFormData = {
  name: `E2E Update Territory ${uniqueSuffix}`,
}
const updatedData: TerritoryFormData = {
  name: `E2E Updated Territory ${uniqueSuffix}`,
}

test.describe.serial('Territory Update', () => {
  let territoryPage: TerritoryPage

  test('setup: create a territory to edit', async ({ page }) => {
    territoryPage = new TerritoryPage(page)
    await territoryPage.goto()
    await territoryPage.openCreateDialog()
    await territoryPage.fillTerritoryForm({ name: originalData.name })
    await territoryPage.selectFirstArea()
    await territoryPage.submitCreateForm()
    await territoryPage.expectSuccessToast('Territory created successfully')
    await territoryPage.expectDialogClosed()
    await territoryPage.search(originalData.name)
    await territoryPage.expectRowExists(originalData.name)
  })

  test('should open edit dialog with pre-filled name', async ({ page }) => {
    territoryPage = new TerritoryPage(page)
    await territoryPage.goto()
    await territoryPage.search(originalData.name)
    await territoryPage.clickEdit(originalData.name)

    await expect(territoryPage.page.getByRole('heading', { name: 'Edit Territory' })).toBeVisible()
    const dialog = territoryPage.page.locator('[role="dialog"]')
    await expect(dialog.getByLabel('Name', { exact: true })).toHaveValue(originalData.name)
  })

  test('should show validation error when name is cleared in edit mode', async ({ page }) => {
    territoryPage = new TerritoryPage(page)
    await territoryPage.goto()
    await territoryPage.search(originalData.name)
    await territoryPage.clickEdit(originalData.name)

    await territoryPage.fillTerritoryForm({ name: '' })
    await territoryPage.submitEditForm()
    await territoryPage.expectFieldError('Name is required')
    await expect(territoryPage.page.getByRole('dialog')).toBeVisible()
  })

  test('should update territory name successfully', async ({ page }) => {
    territoryPage = new TerritoryPage(page)
    await territoryPage.goto()
    await territoryPage.search(originalData.name)
    await territoryPage.clickEdit(originalData.name)

    await territoryPage.fillTerritoryForm({ name: updatedData.name })
    await territoryPage.submitEditForm()

    await territoryPage.expectSuccessToast('Territory updated successfully')
    await territoryPage.expectDialogClosed()
    await territoryPage.search(updatedData.name)
    await territoryPage.expectRowExists(updatedData.name)
    await territoryPage.expectRowNotExists(originalData.name)
  })
})
