import { test, expect } from '@playwright/test'
import { PricingStructurePage, type PricingStructureFormData } from '../../pages/pricing-structure.page'

const uniqueSuffix = Date.now().toString(36)

const testStructure: PricingStructureFormData = {
  name: `E2E ACT ${uniqueSuffix}`,
  description: `Created for activate test`,
  isDefault: false,
}

test.describe.serial('Activate Pricing Structure', () => {
  test('setup: create and deactivate a pricing structure', async ({ page }) => {
    const pricingStructurePage = new PricingStructurePage(page)
    await pricingStructurePage.goto()

    // Create
    await pricingStructurePage.openCreateDialog()
    await pricingStructurePage.fillForm(testStructure)
    await pricingStructurePage.submitCreateForm()
    await pricingStructurePage.expectSuccessToast()
    await pricingStructurePage.expectDialogClosed()

    // Deactivate so we can test activation
    await pricingStructurePage.search(testStructure.name)
    await pricingStructurePage.expectRowExists(testStructure.name)
    await pricingStructurePage.clickDeactivate(testStructure.name)
    await pricingStructurePage.confirmDeactivate()
    await pricingStructurePage.expectSuccessToast('Pricing structure deactivated successfully')
    await pricingStructurePage.expectRowStatus(testStructure.name, 'Inactive')
  })

  test('should show activate confirmation dialog', async ({ page }) => {
    const pricingStructurePage = new PricingStructurePage(page)
    await pricingStructurePage.goto()

    await pricingStructurePage.search(testStructure.name)
    await pricingStructurePage.expectRowExists(testStructure.name)

    await pricingStructurePage.clickActivate(testStructure.name)

    const alertDialog = page.getByRole('alertdialog')
    await expect(alertDialog).toBeVisible()
    await expect(alertDialog.getByText('Activate Pricing Structure')).toBeVisible()
    await expect(alertDialog.getByText(/active/i)).toBeVisible()
  })

  test('should cancel activation and pricing structure remains Inactive', async ({ page }) => {
    const pricingStructurePage = new PricingStructurePage(page)
    await pricingStructurePage.goto()

    await pricingStructurePage.search(testStructure.name)
    await pricingStructurePage.expectRowExists(testStructure.name)

    await pricingStructurePage.clickActivate(testStructure.name)
    await pricingStructurePage.cancelAlert()

    // Pricing structure should still be Inactive
    await pricingStructurePage.expectRowStatus(testStructure.name, 'Inactive')
  })

  test('should activate pricing structure successfully and show Active status', async ({
    page,
  }) => {
    const pricingStructurePage = new PricingStructurePage(page)
    await pricingStructurePage.goto()

    await pricingStructurePage.search(testStructure.name)
    await pricingStructurePage.expectRowExists(testStructure.name)
    await pricingStructurePage.expectRowStatus(testStructure.name, 'Inactive')

    await pricingStructurePage.clickActivate(testStructure.name)
    await pricingStructurePage.confirmActivate()

    // Success toast confirms API completed
    await pricingStructurePage.expectSuccessToast('Pricing structure activated successfully')

    // Row should be Active
    await pricingStructurePage.search(testStructure.name)
    await pricingStructurePage.expectRowExists(testStructure.name)
    await pricingStructurePage.expectRowStatus(testStructure.name, 'Active')
  })

  test('should show Deactivate menu item instead of Activate after activation', async ({
    page,
  }) => {
    const pricingStructurePage = new PricingStructurePage(page)
    await pricingStructurePage.goto()

    await pricingStructurePage.search(testStructure.name)
    await pricingStructurePage.expectRowExists(testStructure.name)

    await pricingStructurePage.openRowActions(testStructure.name)

    // Should show Deactivate (not Activate) for active row
    await expect(page.getByRole('menuitem', { name: 'Deactivate', exact: true })).toBeVisible()
    await expect(page.getByRole('menuitem', { name: 'Activate', exact: true })).not.toBeVisible()
  })
})
