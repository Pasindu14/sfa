import { test, expect } from '@playwright/test'
import { TerritoryPage, type TerritoryFormData } from '../../pages/territory.page'

const uniqueSuffix = Date.now().toString(36)
const testData: TerritoryFormData = {
  name: `E2E Territory ${uniqueSuffix}`,
}

test.describe('Territory Create', () => {
  let territoryPage: TerritoryPage

  test.beforeEach(async ({ page }) => {
    territoryPage = new TerritoryPage(page)
    await territoryPage.goto()
  })

  test('should open create dialog when Add Territory is clicked', async () => {
    await territoryPage.openCreateDialog()
    await expect(territoryPage.page.getByRole('dialog')).toBeVisible()
    await expect(territoryPage.page.getByText('Add a new territory to the system.')).toBeVisible()
  })

  test('should close dialog on cancel', async () => {
    await territoryPage.openCreateDialog()
    await territoryPage.page.keyboard.press('Escape')
    await territoryPage.expectDialogClosed()
  })

  test('should show validation error when name is empty', async () => {
    await territoryPage.openCreateDialog()
    await territoryPage.submitCreateForm()
    await territoryPage.expectFieldError('Name is required')
    await expect(territoryPage.page.getByRole('dialog')).toBeVisible()
  })

  test('should show validation error when area is not selected', async () => {
    await territoryPage.openCreateDialog()
    await territoryPage.fillTerritoryForm({ name: testData.name })
    await territoryPage.submitCreateForm()
    await territoryPage.expectFieldError('Area is required')
    await expect(territoryPage.page.getByRole('dialog')).toBeVisible()
  })

  test('should show validation error when name exceeds 100 characters', async () => {
    await territoryPage.openCreateDialog()
    await territoryPage.fillTerritoryForm({ name: 'A'.repeat(101) })
    await territoryPage.submitCreateForm()
    await territoryPage.expectFieldError('Name must not exceed 100 characters')
    await expect(territoryPage.page.getByRole('dialog')).toBeVisible()
  })

  test('should create a territory successfully', async () => {
    await territoryPage.openCreateDialog()
    await territoryPage.fillTerritoryForm({ name: testData.name })
    await territoryPage.selectFirstArea()
    await territoryPage.submitCreateForm()

    await territoryPage.expectSuccessToast('Territory created successfully')
    await territoryPage.expectDialogClosed()
    await territoryPage.search(testData.name)
    await territoryPage.expectRowExists(testData.name)
  })

  test('should show error when creating a duplicate territory name', async () => {
    const dupName = `E2E Dup Territory ${Date.now().toString(36)}`

    // First creation — should succeed
    await territoryPage.openCreateDialog()
    await territoryPage.fillTerritoryForm({ name: dupName })
    await territoryPage.selectFirstArea()
    await territoryPage.submitCreateForm()
    await territoryPage.expectSuccessToast('Territory created successfully')
    await territoryPage.expectDialogClosed()

    // Second creation with the same name — should conflict
    await territoryPage.openCreateDialog()
    await territoryPage.fillTerritoryForm({ name: dupName })
    await territoryPage.selectFirstArea()
    await territoryPage.submitCreateForm()

    // Dialog stays open on conflict
    await expect(territoryPage.page.getByRole('dialog')).toBeVisible()
  })

  test('newly created territory should show Active status badge', async () => {
    const localName = `E2E Badge Territory ${Date.now().toString(36)}`

    await territoryPage.openCreateDialog()
    await territoryPage.fillTerritoryForm({ name: localName })
    await territoryPage.selectFirstArea()
    await territoryPage.submitCreateForm()
    await territoryPage.expectSuccessToast()
    await territoryPage.expectDialogClosed()

    await territoryPage.search(localName)
    await territoryPage.expectRowStatus(localName, 'Active')
  })
})
