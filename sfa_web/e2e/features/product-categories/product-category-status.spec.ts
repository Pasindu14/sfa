import { test, expect } from '@playwright/test'
import { ProductCategoryPage } from '../../pages/product-category.page'

const uid = Date.now().toString(36)
const CATEGORY_NAME = `E2E Cat Stat ${uid}`

test.describe.serial('Activate / Deactivate Product Category', () => {
  test.setTimeout(60_000)

  test('setup: create a category to toggle status', async ({ page }) => {
    const catPage = new ProductCategoryPage(page)
    await catPage.goto()

    await catPage.openCreateDialog()
    await catPage.fillForm({ name: CATEGORY_NAME })
    await catPage.submitCreateForm()

    await catPage.expectSuccessToast('Product category created successfully')
    await catPage.expectDialogClosed()
  })

  test('should open deactivate confirmation dialog', async ({ page }) => {
    const catPage = new ProductCategoryPage(page)
    await catPage.goto()

    await catPage.search(CATEGORY_NAME)
    await catPage.expectRowStatus(CATEGORY_NAME, 'Active')
    await catPage.clickDeactivate(CATEGORY_NAME)

    await expect(page.getByRole('alertdialog')).toBeVisible({ timeout: 10_000 })
    await catPage.cancelAlert()
  })

  test('should cancel deactivate and keep category Active', async ({ page }) => {
    const catPage = new ProductCategoryPage(page)
    await catPage.goto()

    await catPage.search(CATEGORY_NAME)
    await catPage.expectRowStatus(CATEGORY_NAME, 'Active')
    await catPage.clickDeactivate(CATEGORY_NAME)
    await catPage.cancelAlert()

    await catPage.expectRowStatus(CATEGORY_NAME, 'Active')
  })

  test('should deactivate category successfully', async ({ page }) => {
    const catPage = new ProductCategoryPage(page)
    await catPage.goto()

    await catPage.search(CATEGORY_NAME)
    await catPage.expectRowExists(CATEGORY_NAME)
    await catPage.expectRowStatus(CATEGORY_NAME, 'Active')

    await catPage.clickDeactivate(CATEGORY_NAME)
    await catPage.confirmAlertAction('Deactivate')

    await catPage.expectSuccessToast('Product category deactivated successfully')
    await catPage.expectRowStatus(CATEGORY_NAME, 'Inactive')
  })

  test('should show Activate menu item for inactive category', async ({ page }) => {
    const catPage = new ProductCategoryPage(page)
    await catPage.goto()

    await catPage.search(CATEGORY_NAME)
    await catPage.expectRowStatus(CATEGORY_NAME, 'Inactive')

    await catPage.openRowActions(CATEGORY_NAME)
    await expect(page.getByRole('menuitem', { name: 'Activate' })).toBeVisible()
    await expect(page.getByRole('menuitem', { name: 'Deactivate' })).not.toBeVisible()
    // Close the menu
    await page.keyboard.press('Escape')
  })

  test('should activate category successfully', async ({ page }) => {
    const catPage = new ProductCategoryPage(page)
    await catPage.goto()

    await catPage.search(CATEGORY_NAME)
    await catPage.expectRowExists(CATEGORY_NAME)

    await catPage.clickActivate(CATEGORY_NAME)
    await catPage.confirmAlertAction('Activate')

    await catPage.expectSuccessToast('Product category activated successfully')
    await catPage.expectRowStatus(CATEGORY_NAME, 'Active')
  })
})
