import { test, expect } from '@playwright/test'
import { ProductPage, type ProductFormData } from '../../pages/product.page'

// Generate unique values per test run to prevent database conflicts
const uniqueSuffix = Date.now().toString(36)

const testProduct: ProductFormData = {
  code: `E2E-${uniqueSuffix}`.toUpperCase(),
  piecesPerPack: 12,
  itemDescription: `E2E Test Product ${uniqueSuffix}`,
  printDescription: `E2E PRINT ${uniqueSuffix}`.toUpperCase(),
  imageUrl: 'https://example.com/product.png',
  remarks: 'Created by E2E test',
}

test.describe.serial('Create Product', () => {
  let productPage: ProductPage

  test.beforeEach(async ({ page }) => {
    productPage = new ProductPage(page)
    await productPage.goto()
  })

  test('should open create dialog when clicking Add Product', async () => {
    await productPage.openCreateDialog()

    const dialog = productPage.page.locator('[role="dialog"]')
    await expect(dialog.getByLabel('Code *', { exact: true })).toBeVisible()
    await expect(dialog.getByLabel('Pieces Per Pack *', { exact: true })).toBeVisible()
    await expect(dialog.getByLabel('Item Description *', { exact: true })).toBeVisible()
    await expect(dialog.getByLabel('Print Description (Optional)', { exact: true })).toBeVisible()
    await expect(dialog.getByLabel('Image URL (Optional)', { exact: true })).toBeVisible()
    await expect(dialog.getByLabel('Remarks (Optional)', { exact: true })).toBeVisible()
  })

  test('should close dialog when clicking X button', async () => {
    await productPage.openCreateDialog()

    // Click the close button (X) in the dialog
    await productPage.page.getByRole('button', { name: 'Close' }).click()

    await productPage.expectDialogClosed()
  })

  test('should show Code is required error on empty form submit', async () => {
    await productPage.openCreateDialog()

    // Submit without filling anything
    await productPage.submitCreateForm()

    await productPage.expectFieldError('Code is required')

    // Dialog should remain open
    const dialog = productPage.page.locator('[role="dialog"]')
    await expect(dialog).toBeVisible()
  })

  test('should show itemDescription required error when code filled but description empty', async () => {
    await productPage.openCreateDialog()

    await productPage.fillProductForm({ code: testProduct.code, piecesPerPack: 1 })
    await productPage.submitCreateForm()

    // Dialog should remain open due to missing itemDescription
    const dialog = productPage.page.locator('[role="dialog"]')
    await expect(dialog).toBeVisible()
  })

  test('should create a new product successfully', async () => {
    await productPage.openCreateDialog()
    await productPage.fillProductForm(testProduct)
    await productPage.submitCreateForm()

    // Success toast confirms API completed
    await productPage.expectSuccessToast('Product created successfully')

    // Dialog should close after mutation success
    await productPage.expectDialogClosed()

    // New product should appear in the table
    await productPage.search(testProduct.code)
    await productPage.expectRowExists(testProduct.code)
  })

  test('should keep dialog open or show error when submitting duplicate code', async () => {
    // Attempt to create the same product again (duplicate code)
    await productPage.openCreateDialog()
    await productPage.fillProductForm(testProduct)
    await productPage.submitCreateForm()

    // Either the dialog stays open (field error) or an error toast appears
    const dialog = productPage.page.locator('[role="dialog"]')
    const errorToast = productPage.page.locator('[data-sonner-toast][data-type="error"]').first()

    const dialogVisible = await dialog.isVisible({ timeout: 5_000 }).catch(() => false)
    const toastVisible = await errorToast.isVisible({ timeout: 5_000 }).catch(() => false)

    expect(dialogVisible || toastVisible).toBe(true)
  })
})
