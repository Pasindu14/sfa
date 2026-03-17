import { test, expect } from '@playwright/test'
import { PricingStructurePage } from '../../pages/pricing-structure.page'

test.describe('Pricing Structure List', () => {
  let pricingStructurePage: PricingStructurePage

  test.beforeEach(async ({ page }) => {
    pricingStructurePage = new PricingStructurePage(page)
    await pricingStructurePage.goto()
  })

  test('should display page with correct heading', async () => {
    await expect(
      pricingStructurePage.page.getByRole('heading', { name: 'Pricing Structures' })
    ).toBeVisible()
  })

  test('should display table with correct column headers', async () => {
    await expect(
      pricingStructurePage.page.getByRole('columnheader', { name: 'Name' })
    ).toBeVisible()
    await expect(
      pricingStructurePage.page.getByRole('columnheader', { name: 'Description' })
    ).toBeVisible()
    await expect(
      pricingStructurePage.page.getByRole('columnheader', { name: 'Products' })
    ).toBeVisible()
    await expect(
      pricingStructurePage.page.getByRole('columnheader', { name: 'Status' })
    ).toBeVisible()
  })

  test('should have Add Pricing Structure button visible', async () => {
    await expect(pricingStructurePage.addButton).toBeVisible()
  })

  test('should have search input with correct placeholder', async () => {
    await expect(pricingStructurePage.searchInput).toBeVisible()
    await expect(pricingStructurePage.searchInput).toHaveAttribute(
      'placeholder',
      'Search pricing structures...'
    )
  })

  test('should search for an existing pricing structure and show results', async () => {
    await pricingStructurePage.expectTableHasRows()

    const firstNameCell = await pricingStructurePage.table
      .locator('tbody tr')
      .first()
      .locator('span.text-sm.font-medium')
      .textContent()

    if (firstNameCell) {
      await pricingStructurePage.search(firstNameCell.trim())
      await pricingStructurePage.expectRowExists(firstNameCell.trim())
    }
  })

  test('should show empty state when searching for nonexistent pricing structure', async () => {
    await pricingStructurePage.search('zzz_nonexistent_pricing_xyz_999')
    await pricingStructurePage.page.waitForTimeout(1000)

    const rows = pricingStructurePage.table.locator('tbody tr')
    const count = await rows.count()
    expect(count).toBeLessThanOrEqual(1)
  })

  test('should restore table data after clearing search', async () => {
    await pricingStructurePage.search('zzz_nonexistent_pricing_xyz_999')
    await pricingStructurePage.page.waitForTimeout(500)
    await pricingStructurePage.clearSearch()
    await pricingStructurePage.expectTableHasRows()
  })
})
