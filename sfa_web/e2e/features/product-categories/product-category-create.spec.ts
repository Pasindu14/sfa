import { test, expect } from '@playwright/test'
import { ProductCategoryPage } from '../../pages/product-category.page'

const uid = Date.now().toString(36)
const CATEGORY_NAME = `E2E Cat ${uid}`

test.describe.serial('Create Product Category', () => {
  test.setTimeout(60_000)

  test('should open the create category dialog', async ({ page }) => {
    const catPage = new ProductCategoryPage(page)
    await catPage.goto()

    await catPage.openCreateDialog()

    const dialog = page.locator('[role="dialog"]')
    await expect(dialog).toBeVisible()
    await expect(dialog.getByRole('button', { name: 'Create Category' })).toBeVisible()
  })

  test('should show validation error when name is empty', async ({ page }) => {
    const catPage = new ProductCategoryPage(page)
    await catPage.goto()
    await catPage.openCreateDialog()

    await catPage.submitCreateForm()

    // Dialog stays open; zod/server validation fires
    await expect(page.locator('[role="dialog"]')).toBeVisible()
  })

  test('should create a new product category successfully', async ({ page }) => {
    const catPage = new ProductCategoryPage(page)
    await catPage.goto()

    await catPage.openCreateDialog()
    await catPage.fillForm({ name: CATEGORY_NAME })
    await catPage.submitCreateForm()

    await catPage.expectSuccessToast('Product category created successfully')
    await catPage.expectDialogClosed()
  })

  test('should display the new category in the table', async ({ page }) => {
    const catPage = new ProductCategoryPage(page)
    await catPage.goto()

    await catPage.search(CATEGORY_NAME)
    await catPage.expectRowExists(CATEGORY_NAME)
    await catPage.expectRowStatus(CATEGORY_NAME, 'Active')
  })
})
