import { test, expect } from '@playwright/test'
import { RegionPage, type RegionFormData } from '../../pages/region.page'

const uniqueSuffix = Date.now().toString(36)
const testData: RegionFormData = {
  name: `E2E Region ${uniqueSuffix}`,
}

test.describe('Region Create', () => {
  let regionPage: RegionPage

  test.beforeEach(async ({ page }) => {
    regionPage = new RegionPage(page)
    await regionPage.goto()
  })

  test('should open create dialog when Add Region is clicked', async () => {
    await regionPage.openCreateDialog()
    await expect(regionPage.page.getByRole('dialog')).toBeVisible()
    await expect(regionPage.page.getByText('Add a new region to the system.')).toBeVisible()
  })

  test('should close dialog on cancel', async () => {
    await regionPage.openCreateDialog()
    await regionPage.page.keyboard.press('Escape')
    await regionPage.expectDialogClosed()
  })

  test('should show validation error when name is empty', async () => {
    await regionPage.openCreateDialog()
    await regionPage.submitCreateForm()
    await regionPage.expectFieldError('Name is required')
    await expect(regionPage.page.getByRole('dialog')).toBeVisible()
  })

  test('should show validation error when name exceeds 100 characters', async () => {
    await regionPage.openCreateDialog()
    await regionPage.fillRegionForm({ name: 'A'.repeat(101) })
    await regionPage.submitCreateForm()
    await regionPage.expectFieldError('Name must not exceed 100 characters')
    await expect(regionPage.page.getByRole('dialog')).toBeVisible()
  })

  test('should create a region successfully', async () => {
    await regionPage.openCreateDialog()
    await regionPage.fillRegionForm(testData)
    await regionPage.submitCreateForm()

    await regionPage.expectSuccessToast('Region created successfully')
    await regionPage.expectDialogClosed()
    await regionPage.search(testData.name)
    await regionPage.expectRowExists(testData.name)
  })

  test('should show error when creating a duplicate region name', async () => {
    const dupName = `E2E Dup Region ${Date.now().toString(36)}`

    // First creation — should succeed
    await regionPage.openCreateDialog()
    await regionPage.fillRegionForm({ name: dupName })
    await regionPage.submitCreateForm()
    await regionPage.expectSuccessToast('Region created successfully')
    await regionPage.expectDialogClosed()

    // Second creation with the same name — should conflict
    await regionPage.openCreateDialog()
    await regionPage.fillRegionForm({ name: dupName })
    await regionPage.submitCreateForm()

    // Dialog stays open on conflict
    await expect(regionPage.page.getByRole('dialog')).toBeVisible()
  })

  test('newly created region should show Active status badge', async () => {
    const localName = `E2E Badge Region ${Date.now().toString(36)}`

    await regionPage.openCreateDialog()
    await regionPage.fillRegionForm({ name: localName })
    await regionPage.submitCreateForm()
    await regionPage.expectSuccessToast()
    await regionPage.expectDialogClosed()
    await regionPage.search(localName)
    await regionPage.expectRowStatus(localName, 'Active')
  })
})
