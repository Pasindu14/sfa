import { test, expect } from '@playwright/test'
import { ProductPage, type ProductFormData } from '../../pages/product.page'

const uniqueSuffix = Date.now().toString(36)

const testProduct: ProductFormData = {
  code: `E2E-DEACT-${uniqueSuffix}`.toUpperCase(),
  piecesPerPack: 4,
  itemDescription: `E2E Deactivate Product ${uniqueSuffix}`,
}

test.describe.serial('Deactivate Product', () => {
  test('setup: create a product', async ({ page }) => {
    const productPage = new ProductPage(page)
    await productPage.goto()

    await productPage.openCreateDialog()
    await productPage.fillProductForm(testProduct)
    await productPage.submitCreateForm()

    await productPage.expectSuccessToast()
    await productPage.expectDialogClosed()
  })

  test('should show deactivate confirmation dialog mentioning inactive', async ({ page }) => {
    const productPage = new ProductPage(page)
    await productPage.goto()

    await productPage.search(testProduct.code)
    await productPage.expectRowExists(testProduct.code)

    await productPage.clickDeactivate(testProduct.code)

    const alertDialog = page.getByRole('alertdialog')
    await expect(alertDialog).toBeVisible()
    await expect(alertDialog.getByText('Deactivate Product')).toBeVisible()
    // Confirmation text should mention "inactive"
    await expect(alertDialog.getByText(/inactive/i)).toBeVisible()
  })

  test('should cancel deactivation and product remains Active', async ({ page }) => {
    const productPage = new ProductPage(page)
    await productPage.goto()

    await productPage.search(testProduct.code)
    await productPage.expectRowExists(testProduct.code)

    await productPage.clickDeactivate(testProduct.code)
    await productPage.cancelAlert()

    // Product should still be Active
    await productPage.expectRowStatus(testProduct.code, 'Active')
  })

  test('should deactivate product successfully and show Inactive status', async ({ page }) => {
    const productPage = new ProductPage(page)
    await productPage.goto()

    await productPage.search(testProduct.code)
    await productPage.expectRowExists(testProduct.code)
    await productPage.expectRowStatus(testProduct.code, 'Active')

    await productPage.clickDeactivate(testProduct.code)
    await productPage.confirmDeactivate()

    // Success toast confirms API completed
    await productPage.expectSuccessToast('Product deactivated successfully')

    // Row should still be visible but with Inactive status
    await productPage.search(testProduct.code)
    await productPage.expectRowExists(testProduct.code)
    await productPage.expectRowStatus(testProduct.code, 'Inactive')
  })
})
