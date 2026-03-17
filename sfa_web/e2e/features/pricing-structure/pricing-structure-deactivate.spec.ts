import { test, expect } from '@playwright/test'
import { PricingStructurePage, type PricingStructureFormData } from '../../pages/pricing-structure.page'

const uniqueSuffix = Date.now().toString(36)

const testStructure: PricingStructureFormData = {
  name: `E2E DEACT ${uniqueSuffix}`,
  description: `Created for deactivate test`,
  isDefault: false,
}

test.describe.serial('Deactivate Pricing Structure', () => {
  test('setup: create a pricing structure', async ({ page }) => {
    const pricingStructurePage = new PricingStructurePage(page)
    await pricingStructurePage.goto()

    await pricingStructurePage.openCreateDialog()
    await pricingStructurePage.fillForm(testStructure)
    await pricingStructurePage.submitCreateForm()

    await pricingStructurePage.expectSuccessToast()
    await pricingStructurePage.expectDialogClosed()
  })

  test('should show deactivate confirmation dialog', async ({ page }) => {
    const pricingStructurePage = new PricingStructurePage(page)
    await pricingStructurePage.goto()

    await pricingStructurePage.search(testStructure.name)
    await pricingStructurePage.expectRowExists(testStructure.name)

    await pricingStructurePage.clickDeactivate(testStructure.name)

    const alertDialog = page.getByRole('alertdialog')
    await expect(alertDialog).toBeVisible()
    await expect(alertDialog.getByText('Deactivate Pricing Structure')).toBeVisible()
    await expect(alertDialog.getByText(/deactivated/i)).toBeVisible()
  })

  test('should cancel deactivation and pricing structure remains Active', async ({ page }) => {
    const pricingStructurePage = new PricingStructurePage(page)
    await pricingStructurePage.goto()

    await pricingStructurePage.search(testStructure.name)
    await pricingStructurePage.expectRowExists(testStructure.name)

    await pricingStructurePage.clickDeactivate(testStructure.name)
    await pricingStructurePage.cancelAlert()

    // Pricing structure should still be Active
    await pricingStructurePage.expectRowStatus(testStructure.name, 'Active')
  })

  test('should deactivate pricing structure successfully and show Inactive status', async ({
    page,
  }) => {
    const pricingStructurePage = new PricingStructurePage(page)
    await pricingStructurePage.goto()

    await pricingStructurePage.search(testStructure.name)
    await pricingStructurePage.expectRowExists(testStructure.name)
    await pricingStructurePage.expectRowStatus(testStructure.name, 'Active')

    await pricingStructurePage.clickDeactivate(testStructure.name)
    await pricingStructurePage.confirmDeactivate()

    // Success toast confirms API completed
    await pricingStructurePage.expectSuccessToast('Pricing structure deactivated successfully')

    // Row should still be visible but with Inactive status
    await pricingStructurePage.search(testStructure.name)
    await pricingStructurePage.expectRowExists(testStructure.name)
    await pricingStructurePage.expectRowStatus(testStructure.name, 'Inactive')
  })

  test('should show Activate menu item instead of Deactivate after deactivation', async ({
    page,
  }) => {
    const pricingStructurePage = new PricingStructurePage(page)
    await pricingStructurePage.goto()

    await pricingStructurePage.search(testStructure.name)
    await pricingStructurePage.expectRowExists(testStructure.name)

    await pricingStructurePage.openRowActions(testStructure.name)

    // Should show Activate (not Deactivate) for inactive row
    await expect(page.getByRole('menuitem', { name: 'Activate', exact: true })).toBeVisible()
    await expect(page.getByRole('menuitem', { name: 'Deactivate', exact: true })).not.toBeVisible()
  })
})
