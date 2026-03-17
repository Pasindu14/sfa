import { test, expect } from '@playwright/test'
import { ProductPage, type ProductFormData } from '../../pages/product.page'

const uniqueSuffix = Date.now().toString(36)

const testProduct: ProductFormData = {
  code: `E2E-ACT-${uniqueSuffix}`.toUpperCase(),
  piecesPerPack: 4,
  itemDescription: `E2E Activate Product ${uniqueSuffix}`,
}

test.describe.serial('Activate Product', () => {
  test('setup: create and deactivate a product', async ({ page }) => {
    const productPage = new ProductPage(page)
    await productPage.goto()

    // Create
    await productPage.openCreateDialog()
    await productPage.fillProductForm(testProduct)
    await productPage.submitCreateForm()
    await productPage.expectSuccessToast()
    await productPage.expectDialogClosed()

    // Deactivate so we can test activation
    await productPage.search(testProduct.code)
    await productPage.expectRowExists(testProduct.code)
    await productPage.clickDeactivate(testProduct.code)
    await productPage.confirmDeactivate()
    await productPage.expectSuccessToast('Product deactivated successfully')
    await productPage.expectRowStatus(testProduct.code, 'Inactive')
  })

  test('should show activate confirmation dialog', async ({ page }) => {
    const productPage = new ProductPage(page)
    await productPage.goto()

    await productPage.search(testProduct.code)
    await productPage.expectRowExists(testProduct.code)

    await productPage.clickActivate(testProduct.code)

    const alertDialog = page.getByRole('alertdialog')
    await expect(alertDialog).toBeVisible()
    await expect(alertDialog.getByText('Activate Product')).toBeVisible()
    await expect(alertDialog.getByText(/active/i)).toBeVisible()
  })

  test('should cancel activation and product remains Inactive', async ({ page }) => {
    const productPage = new ProductPage(page)
    await productPage.goto()

    await productPage.search(testProduct.code)
    await productPage.expectRowExists(testProduct.code)

    await productPage.clickActivate(testProduct.code)
    await productPage.cancelAlert()

    await productPage.expectRowStatus(testProduct.code, 'Inactive')
  })

  test('should activate product successfully and show Active status', async ({ page }) => {
    const productPage = new ProductPage(page)
    await productPage.goto()

    await productPage.search(testProduct.code)
    await productPage.expectRowExists(testProduct.code)
    await productPage.expectRowStatus(testProduct.code, 'Inactive')

    await productPage.clickActivate(testProduct.code)
    await productPage.confirmActivate()

    await productPage.expectSuccessToast('Product activated successfully')

    await productPage.search(testProduct.code)
    await productPage.expectRowExists(testProduct.code)
    await productPage.expectRowStatus(testProduct.code, 'Active')
  })

  test('should show Deactivate menu item instead of Activate after activation', async ({ page }) => {
    const productPage = new ProductPage(page)
    await productPage.goto()

    await productPage.search(testProduct.code)
    await productPage.expectRowExists(testProduct.code)

    await productPage.openRowActions(testProduct.code)

    await expect(page.getByRole('menuitem', { name: 'Deactivate', exact: true })).toBeVisible()
    await expect(page.getByRole('menuitem', { name: 'Activate', exact: true })).not.toBeVisible()
  })
})
