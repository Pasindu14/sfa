import { test, expect } from '@playwright/test'
import { ProductCategoryPage } from '../../pages/product-category.page'

const uid = Date.now().toString(36)
const ORIGINAL_NAME = `E2E Cat Upd ${uid}`
const UPDATED_NAME  = `E2E Cat Upd ${uid} EDITED`

test.describe.serial('Update Product Category', () => {
  test.setTimeout(60_000)

  test('setup: create a category to edit', async ({ page }) => {
    const catPage = new ProductCategoryPage(page)
    await catPage.goto()

    await catPage.openCreateDialog()
    await catPage.fillForm({ name: ORIGINAL_NAME })
    await catPage.submitCreateForm()

    await catPage.expectSuccessToast('Product category created successfully')
    await catPage.expectDialogClosed()
  })

  test('should open the edit dialog with pre-filled name', async ({ page }) => {
    const catPage = new ProductCategoryPage(page)
    await catPage.goto()

    await catPage.search(ORIGINAL_NAME)
    await catPage.expectRowExists(ORIGINAL_NAME)
    await catPage.clickEdit(ORIGINAL_NAME)

    const dialog = page.locator('[role="dialog"]')
    await expect(dialog).toBeVisible()
    // Name field should be pre-filled with the current name
    await expect(dialog.getByLabel('Name')).toHaveValue(ORIGINAL_NAME)
  })

  test('should update the category name successfully', async ({ page }) => {
    const catPage = new ProductCategoryPage(page)
    await catPage.goto()

    await catPage.search(ORIGINAL_NAME)
    await catPage.expectRowExists(ORIGINAL_NAME)
    await catPage.clickEdit(ORIGINAL_NAME)

    await catPage.fillForm({ name: UPDATED_NAME })
    await catPage.submitEditForm()

    await catPage.expectSuccessToast('Product category updated successfully')
    await catPage.expectDialogClosed()
  })

  test('should display the updated name in the table', async ({ page }) => {
    const catPage = new ProductCategoryPage(page)
    await catPage.goto()

    await catPage.search(UPDATED_NAME)
    await catPage.expectRowExists(UPDATED_NAME)
  })
})
