import { test, expect } from '@playwright/test'
import { PricingStructurePage, type PricingStructureFormData } from '../../pages/pricing-structure.page'

const uniqueSuffix = Date.now().toString(36)

const testStructure: PricingStructureFormData = {
  name: `E2E ITEMS ${uniqueSuffix}`,
  description: 'Created for manage items test',
  isDefault: false,
}

test.describe.serial('Manage Items Dialog', () => {
  test('setup: create a pricing structure', async ({ page }) => {
    const pricingStructurePage = new PricingStructurePage(page)
    await pricingStructurePage.goto()

    await pricingStructurePage.openCreateDialog()
    await pricingStructurePage.fillForm(testStructure)
    await pricingStructurePage.submitCreateForm()

    await pricingStructurePage.expectSuccessToast()
    await pricingStructurePage.expectDialogClosed()
  })

  test('should open Manage Items dialog from row actions', async ({ page }) => {
    const pricingStructurePage = new PricingStructurePage(page)
    await pricingStructurePage.goto()

    await pricingStructurePage.search(testStructure.name)
    await pricingStructurePage.expectRowExists(testStructure.name)
    await pricingStructurePage.clickManageItems(testStructure.name)

    const dialog = page.locator('[role="dialog"]')
    await expect(dialog.getByRole('heading', { name: 'Manage Items' })).toBeVisible()
  })

  test('should display column headers inside the dialog', async ({ page }) => {
    const pricingStructurePage = new PricingStructurePage(page)
    await pricingStructurePage.goto()

    await pricingStructurePage.search(testStructure.name)
    await pricingStructurePage.clickManageItems(testStructure.name)

    const dialog = page.locator('[role="dialog"]')
    await expect(dialog.getByText('Product', { exact: true })).toBeVisible()
    await expect(dialog.getByText('Dealer Pack Price', { exact: true })).toBeVisible()
    await expect(dialog.getByText('Dealer Case Price', { exact: true })).toBeVisible()
    await expect(dialog.getByText('Promotional Price', { exact: true })).toBeVisible()
  })

  test('should display Save All and Cancel buttons', async ({ page }) => {
    const pricingStructurePage = new PricingStructurePage(page)
    await pricingStructurePage.goto()

    await pricingStructurePage.search(testStructure.name)
    await pricingStructurePage.clickManageItems(testStructure.name)

    const dialog = page.locator('[role="dialog"]')
    await expect(dialog.getByRole('button', { name: 'Save All' })).toBeVisible()
    await expect(dialog.getByRole('button', { name: 'Cancel' })).toBeVisible()
  })

  test('should display a list of products with price inputs', async ({ page }) => {
    const pricingStructurePage = new PricingStructurePage(page)
    await pricingStructurePage.goto()

    await pricingStructurePage.search(testStructure.name)
    await pricingStructurePage.clickManageItems(testStructure.name)

    const dialog = page.locator('[role="dialog"]')
    await dialog.waitFor({ state: 'visible' })

    // Wait for products to load (spinner disappears)
    await expect(dialog.locator('input[type="number"]').first()).toBeVisible({ timeout: 10_000 })

    // There should be at least one product row with number inputs
    const inputs = dialog.locator('input[type="number"]')
    const count = await inputs.count()
    expect(count).toBeGreaterThan(0)
    // Each product contributes 3 inputs (dealer pack, dealer case, promotional)
    expect(count % 3).toBe(0)
  })

  test('should close dialog when clicking Cancel without saving', async ({ page }) => {
    const pricingStructurePage = new PricingStructurePage(page)
    await pricingStructurePage.goto()

    await pricingStructurePage.search(testStructure.name)
    await pricingStructurePage.clickManageItems(testStructure.name)

    const dialog = page.locator('[role="dialog"]')
    await expect(dialog.getByRole('heading', { name: 'Manage Items' })).toBeVisible()

    await dialog.getByRole('button', { name: 'Cancel' }).click()

    await pricingStructurePage.expectDialogClosed()
  })

  test('should save prices for the first product and show success toast', async ({ page }) => {
    const pricingStructurePage = new PricingStructurePage(page)
    await pricingStructurePage.goto()

    await pricingStructurePage.search(testStructure.name)
    await pricingStructurePage.clickManageItems(testStructure.name)

    const dialog = page.locator('[role="dialog"]')

    // Wait for products to load
    await expect(dialog.locator('input[type="number"]').first()).toBeVisible({ timeout: 10_000 })

    // Fill prices for the first product row (inputs 0, 1, 2)
    const inputs = dialog.locator('input[type="number"]')
    await inputs.nth(0).fill('10.50') // dealer pack price
    await inputs.nth(1).fill('95.00') // dealer case price
    await inputs.nth(2).fill('8.99')  // promotional price

    await dialog.getByRole('button', { name: 'Save All' }).click()

    await pricingStructurePage.expectSuccessToast('Pricing structure items updated successfully')
    await pricingStructurePage.expectDialogClosed()
  })

  test('should reflect updated item count in the table after saving', async ({ page }) => {
    const pricingStructurePage = new PricingStructurePage(page)
    await pricingStructurePage.goto()

    await pricingStructurePage.search(testStructure.name)
    await pricingStructurePage.expectRowExists(testStructure.name)

    // The Products column should show at least 1 after the previous test saved prices
    const row = pricingStructurePage.getRowByName(testStructure.name)
    const productsCell = row.locator('div').filter({ hasText: /^\d+$/ }).first()
    const countText = await productsCell.textContent()
    expect(Number(countText?.trim())).toBeGreaterThanOrEqual(1)
  })

  test('should reopen dialog and show pre-filled prices from last save', async ({ page }) => {
    const pricingStructurePage = new PricingStructurePage(page)
    await pricingStructurePage.goto()

    await pricingStructurePage.search(testStructure.name)
    await pricingStructurePage.clickManageItems(testStructure.name)

    const dialog = page.locator('[role="dialog"]')
    await expect(dialog.locator('input[type="number"]').first()).toBeVisible({ timeout: 10_000 })

    // First input (dealer pack price for first product) should retain the saved value
    const firstInput = dialog.locator('input[type="number"]').nth(0)
    await expect(firstInput).toHaveValue('10.5')

    await dialog.getByRole('button', { name: 'Cancel' }).click()
    await pricingStructurePage.expectDialogClosed()
  })
})
