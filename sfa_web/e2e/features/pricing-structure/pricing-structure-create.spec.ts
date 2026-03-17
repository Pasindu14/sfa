import { test, expect } from '@playwright/test'
import { PricingStructurePage, type PricingStructureFormData } from '../../pages/pricing-structure.page'

// Generate unique values per test run to prevent database conflicts
const uniqueSuffix = Date.now().toString(36)

const testStructure: PricingStructureFormData = {
  name: `E2E Pricing ${uniqueSuffix}`,
  description: `Created by E2E test run ${uniqueSuffix}`,
  isDefault: false,
}

test.describe.serial('Create Pricing Structure', () => {
  let pricingStructurePage: PricingStructurePage

  test.beforeEach(async ({ page }) => {
    pricingStructurePage = new PricingStructurePage(page)
    await pricingStructurePage.goto()
  })

  test('should open create dialog when clicking Add Pricing Structure', async () => {
    await pricingStructurePage.openCreateDialog()

    const dialog = pricingStructurePage.page.locator('[role="dialog"]')
    await expect(dialog.getByLabel('Name', { exact: true })).toBeVisible()
    await expect(dialog.getByLabel('Description', { exact: true })).toBeVisible()
    await expect(dialog.getByRole('checkbox')).toBeVisible()
    await expect(dialog.getByText('Set as default')).toBeVisible()
  })

  test('should close dialog when clicking X button', async () => {
    await pricingStructurePage.openCreateDialog()

    await pricingStructurePage.page.getByRole('button', { name: 'Close' }).click()

    await pricingStructurePage.expectDialogClosed()
  })

  test('should show Name is required error on empty form submit', async () => {
    await pricingStructurePage.openCreateDialog()

    // Submit without filling anything
    await pricingStructurePage.submitCreateForm()

    await pricingStructurePage.expectFieldError('Name is required')

    // Dialog should remain open
    const dialog = pricingStructurePage.page.locator('[role="dialog"]')
    await expect(dialog).toBeVisible()
  })

  test('should create a new pricing structure successfully', async () => {
    await pricingStructurePage.openCreateDialog()
    await pricingStructurePage.fillForm(testStructure)
    await pricingStructurePage.submitCreateForm()

    // Success toast confirms API completed
    await pricingStructurePage.expectSuccessToast('Pricing structure created successfully')

    // Dialog should close after mutation success
    await pricingStructurePage.expectDialogClosed()

    // New pricing structure should appear in the table
    await pricingStructurePage.search(testStructure.name)
    await pricingStructurePage.expectRowExists(testStructure.name)
  })

  test('should show newly created pricing structure as Active', async () => {
    await pricingStructurePage.search(testStructure.name)
    await pricingStructurePage.expectRowExists(testStructure.name)
    await pricingStructurePage.expectRowStatus(testStructure.name, 'Active')
  })

  test('should keep dialog open or show error when submitting duplicate name', async () => {
    // Attempt to create the same pricing structure again (duplicate name)
    await pricingStructurePage.openCreateDialog()
    await pricingStructurePage.fillForm(testStructure)
    await pricingStructurePage.submitCreateForm()

    // Either the dialog stays open (field error) or an error toast appears
    const dialog = pricingStructurePage.page.locator('[role="dialog"]')
    const errorToast = pricingStructurePage.page
      .locator('[data-sonner-toast][data-type="error"]')
      .first()

    const dialogVisible = await dialog.isVisible({ timeout: 5_000 }).catch(() => false)
    const toastVisible = await errorToast.isVisible({ timeout: 5_000 }).catch(() => false)

    expect(dialogVisible || toastVisible).toBe(true)
  })
})
