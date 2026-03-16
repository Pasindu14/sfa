import { test, expect } from '@playwright/test'
import { ProductPage, type ProductFormData } from '../../pages/product.page'

const uniqueSuffix = Date.now().toString(36)

const testProduct: ProductFormData = {
  code: `E2E-UPD-${uniqueSuffix}`.toUpperCase(),
  piecesPerPack: 6,
  itemDescription: `E2E Update Product ${uniqueSuffix}`,
  printDescription: 'E2E UPDATE PRINT',
  remarks: 'Created for update test',
}

test.describe.serial('Update Product', () => {
  test('setup: create a product to edit', async ({ page }) => {
    const productPage = new ProductPage(page)
    await productPage.goto()

    await productPage.openCreateDialog()
    await productPage.fillProductForm(testProduct)
    await productPage.submitCreateForm()

    await productPage.expectSuccessToast()
    await productPage.expectDialogClosed()
  })

  test('should open edit dialog with pre-filled values', async ({ page }) => {
    const productPage = new ProductPage(page)
    await productPage.goto()

    await productPage.search(testProduct.code)
    await productPage.expectRowExists(testProduct.code)

    await productPage.clickEdit(testProduct.code)

    const dialog = page.locator('[role="dialog"]')
    await expect(dialog.getByRole('heading', { name: 'Edit Product' })).toBeVisible()

    // Form should be pre-filled with existing values
    const codeInput = dialog.getByLabel('Code *', { exact: true })
    await expect(codeInput).toHaveValue(testProduct.code)

    const descInput = dialog.getByLabel('Item Description *', { exact: true })
    await expect(descInput).toHaveValue(testProduct.itemDescription)
  })

  test('should show validation error when clearing required itemDescription field', async ({ page }) => {
    const productPage = new ProductPage(page)
    await productPage.goto()

    await productPage.search(testProduct.code)
    await productPage.expectRowExists(testProduct.code)

    await productPage.clickEdit(testProduct.code)

    // Clear the itemDescription field and submit
    await productPage.fillProductForm({ itemDescription: '' })
    await productPage.submitEditForm()

    // Dialog should remain open (validation error)
    const dialog = page.locator('[role="dialog"]')
    await expect(dialog).toBeVisible()
  })

  test('should update product itemDescription successfully', async ({ page }) => {
    const productPage = new ProductPage(page)
    await productPage.goto()

    await productPage.search(testProduct.code)
    await productPage.expectRowExists(testProduct.code)

    await productPage.clickEdit(testProduct.code)

    const updatedDescription = `Updated ${testProduct.itemDescription}`
    await productPage.fillProductForm({ itemDescription: updatedDescription })
    await productPage.submitEditForm()

    // Success toast confirms API completed
    await productPage.expectSuccessToast('Product updated successfully')

    // Dialog should close after mutation success
    await productPage.expectDialogClosed()

    // Table should show the updated description (search by code — code does not change)
    await productPage.search(testProduct.code)
    await productPage.expectRowExists(testProduct.code)

    // Verify the updated description text is visible in the row
    const row = productPage.getRowByCode(testProduct.code)
    await expect(row).toContainText(updatedDescription)
  })
})
