import { test, expect } from '@playwright/test'
import { DivisionPage, type DivisionFormData } from '../../pages/division.page'

const uniqueSuffix = Date.now().toString(36)
const testData: DivisionFormData = {
  name: `E2E Division ${uniqueSuffix}`,
}

test.describe('Division Create', () => {
  let divisionPage: DivisionPage

  test.beforeEach(async ({ page }) => {
    divisionPage = new DivisionPage(page)
    await divisionPage.goto()
  })

  test('should open create dialog when Add Division is clicked', async () => {
    await divisionPage.openCreateDialog()
    await expect(divisionPage.page.getByRole('dialog')).toBeVisible()
    await expect(divisionPage.page.getByText('Add a new division to the system.')).toBeVisible()
  })

  test('should close dialog on cancel', async () => {
    await divisionPage.openCreateDialog()
    await divisionPage.page.keyboard.press('Escape')
    await divisionPage.expectDialogClosed()
  })

  test('should show validation error when name is empty', async () => {
    await divisionPage.openCreateDialog()
    await divisionPage.submitCreateForm()
    await divisionPage.expectFieldError('Name is required')
    await expect(divisionPage.page.getByRole('dialog')).toBeVisible()
  })

  test('should show validation error when territory is not selected', async () => {
    await divisionPage.openCreateDialog()
    await divisionPage.fillDivisionForm({ name: testData.name })
    await divisionPage.submitCreateForm()
    await divisionPage.expectFieldError('Territory is required')
    await expect(divisionPage.page.getByRole('dialog')).toBeVisible()
  })

  test('should show validation error when name exceeds 100 characters', async () => {
    await divisionPage.openCreateDialog()
    await divisionPage.fillDivisionForm({ name: 'A'.repeat(101) })
    await divisionPage.submitCreateForm()
    await divisionPage.expectFieldError('Name must not exceed 100 characters')
    await expect(divisionPage.page.getByRole('dialog')).toBeVisible()
  })

  test('should create a division successfully', async () => {
    await divisionPage.openCreateDialog()
    await divisionPage.fillDivisionForm({ name: testData.name })
    await divisionPage.selectFirstTerritory()
    await divisionPage.submitCreateForm()

    await divisionPage.expectSuccessToast('Division created successfully')
    await divisionPage.expectDialogClosed()
    await divisionPage.search(testData.name)
    await divisionPage.expectRowExists(testData.name)
  })

  test('should show error when creating a duplicate division name', async () => {
    const dupName = `E2E Dup Division ${Date.now().toString(36)}`

    // First creation — should succeed
    await divisionPage.openCreateDialog()
    await divisionPage.fillDivisionForm({ name: dupName })
    await divisionPage.selectFirstTerritory()
    await divisionPage.submitCreateForm()
    await divisionPage.expectSuccessToast('Division created successfully')
    await divisionPage.expectDialogClosed()

    // Second creation with the same name — should conflict
    await divisionPage.openCreateDialog()
    await divisionPage.fillDivisionForm({ name: dupName })
    await divisionPage.selectFirstTerritory()
    await divisionPage.submitCreateForm()

    // Dialog stays open on conflict
    await expect(divisionPage.page.getByRole('dialog')).toBeVisible()
  })

  test('newly created division should show Active status badge', async () => {
    const localName = `E2E Badge Division ${Date.now().toString(36)}`

    await divisionPage.openCreateDialog()
    await divisionPage.fillDivisionForm({ name: localName })
    await divisionPage.selectFirstTerritory()
    await divisionPage.submitCreateForm()
    await divisionPage.expectSuccessToast()
    await divisionPage.expectDialogClosed()

    await divisionPage.search(localName)
    await divisionPage.expectRowStatus(localName, 'Active')
  })
})
