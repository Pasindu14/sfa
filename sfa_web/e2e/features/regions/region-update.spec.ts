import { test, expect } from '@playwright/test'
import { RegionPage, type RegionFormData } from '../../pages/region.page'

const uniqueSuffix = Date.now().toString(36) + '1'
const originalData: RegionFormData = {
  name: `E2E Update Region ${uniqueSuffix}`,
}
const updatedData: RegionFormData = {
  name: `E2E Updated Region ${uniqueSuffix}`,
}

test.describe.serial('Region Update', () => {
  let regionPage: RegionPage

  test('setup: create a region to edit', async ({ page }) => {
    regionPage = new RegionPage(page)
    await regionPage.goto()
    await regionPage.openCreateDialog()
    await regionPage.fillRegionForm(originalData)
    await regionPage.submitCreateForm()
    await regionPage.expectSuccessToast('Region created successfully')
    await regionPage.expectDialogClosed()
    await regionPage.search(originalData.name)
    await regionPage.expectRowExists(originalData.name)
  })

  test('should open edit dialog with pre-filled name', async ({ page }) => {
    regionPage = new RegionPage(page)
    await regionPage.goto()
    await regionPage.clickEdit(originalData.name)

    await expect(regionPage.page.getByRole('heading', { name: 'Edit Region' })).toBeVisible()
    const dialog = regionPage.page.locator('[role="dialog"]')
    await expect(dialog.getByLabel('Name', { exact: true })).toHaveValue(originalData.name)
  })

  test('should show validation error when name is cleared in edit mode', async ({ page }) => {
    regionPage = new RegionPage(page)
    await regionPage.goto()
    await regionPage.clickEdit(originalData.name)

    await regionPage.fillRegionForm({ name: '' })
    await regionPage.submitEditForm()
    await regionPage.expectFieldError('Name is required')
    await expect(regionPage.page.getByRole('dialog')).toBeVisible()
  })

  test('should update region name successfully', async ({ page }) => {
    regionPage = new RegionPage(page)
    await regionPage.goto()
    await regionPage.clickEdit(originalData.name)

    await regionPage.fillRegionForm({ name: updatedData.name })
    await regionPage.submitEditForm()

    await regionPage.expectSuccessToast('Region updated successfully')
    await regionPage.expectDialogClosed()
    await regionPage.expectRowExists(updatedData.name)
    await regionPage.expectRowNotExists(originalData.name)
  })
})
