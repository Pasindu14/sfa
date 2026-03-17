import { test, expect } from '@playwright/test'
import { PricingStructurePage, type PricingStructureFormData } from '../../pages/pricing-structure.page'

const uniqueSuffix = Date.now().toString(36)

const testStructure: PricingStructureFormData = {
  name: `E2E UPD ${uniqueSuffix}`,
  description: `Original description ${uniqueSuffix}`,
  isDefault: false,
}

test.describe.serial('Update Pricing Structure', () => {
  test('setup: create a pricing structure to edit', async ({ page }) => {
    const pricingStructurePage = new PricingStructurePage(page)
    await pricingStructurePage.goto()

    await pricingStructurePage.openCreateDialog()
    await pricingStructurePage.fillForm(testStructure)
    await pricingStructurePage.submitCreateForm()

    await pricingStructurePage.expectSuccessToast()
    await pricingStructurePage.expectDialogClosed()
  })

  test('should open edit dialog with pre-filled values', async ({ page }) => {
    const pricingStructurePage = new PricingStructurePage(page)
    await pricingStructurePage.goto()

    await pricingStructurePage.search(testStructure.name)
    await pricingStructurePage.expectRowExists(testStructure.name)

    await pricingStructurePage.clickEdit(testStructure.name)

    const dialog = page.locator('[role="dialog"]')
    await expect(dialog.getByRole('heading', { name: 'Edit Pricing Structure' })).toBeVisible()

    // Form should be pre-filled with existing values
    const nameInput = dialog.getByLabel('Name', { exact: true })
    await expect(nameInput).toHaveValue(testStructure.name)

    const descInput = dialog.getByLabel('Description', { exact: true })
    await expect(descInput).toHaveValue(testStructure.description ?? '')
  })

  test('should show validation error when clearing required Name field', async ({ page }) => {
    const pricingStructurePage = new PricingStructurePage(page)
    await pricingStructurePage.goto()

    await pricingStructurePage.search(testStructure.name)
    await pricingStructurePage.expectRowExists(testStructure.name)

    await pricingStructurePage.clickEdit(testStructure.name)

    // Clear the name field and submit
    await pricingStructurePage.fillForm({ name: '' })
    await pricingStructurePage.submitEditForm()

    // Dialog should remain open (validation error)
    const dialog = page.locator('[role="dialog"]')
    await expect(dialog).toBeVisible()
  })

  test('should update pricing structure description successfully', async ({ page }) => {
    const pricingStructurePage = new PricingStructurePage(page)
    await pricingStructurePage.goto()

    await pricingStructurePage.search(testStructure.name)
    await pricingStructurePage.expectRowExists(testStructure.name)

    await pricingStructurePage.clickEdit(testStructure.name)

    const updatedDescription = `Updated description ${uniqueSuffix}`
    await pricingStructurePage.fillForm({ description: updatedDescription })
    await pricingStructurePage.submitEditForm()

    // Success toast confirms API completed
    await pricingStructurePage.expectSuccessToast('Pricing structure updated successfully')

    // Dialog should close after mutation success
    await pricingStructurePage.expectDialogClosed()

    // Row should still exist (name unchanged)
    await pricingStructurePage.search(testStructure.name)
    await pricingStructurePage.expectRowExists(testStructure.name)

    // Verify updated description is visible in the row
    const row = pricingStructurePage.getRowByName(testStructure.name)
    await expect(row).toContainText(updatedDescription)
  })

  test('should update pricing structure name successfully', async ({ page }) => {
    const pricingStructurePage = new PricingStructurePage(page)
    await pricingStructurePage.goto()

    await pricingStructurePage.search(testStructure.name)
    await pricingStructurePage.expectRowExists(testStructure.name)

    await pricingStructurePage.clickEdit(testStructure.name)

    const updatedName = `E2E UPD RENAMED ${uniqueSuffix}`
    await pricingStructurePage.fillForm({ name: updatedName })
    await pricingStructurePage.submitEditForm()

    await pricingStructurePage.expectSuccessToast('Pricing structure updated successfully')
    await pricingStructurePage.expectDialogClosed()

    // Row should appear under new name
    await pricingStructurePage.search(updatedName)
    await pricingStructurePage.expectRowExists(updatedName)

    // Update in-memory reference for any downstream serial tests
    testStructure.name = updatedName
  })
})
