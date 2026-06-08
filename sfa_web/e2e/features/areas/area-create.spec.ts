import { test, expect } from '@playwright/test'
import { AreaPage, type AreaFormData } from '../../pages/area.page'

const uniqueSuffix = Date.now().toString(36)
const testData: AreaFormData = {
  name: `E2E Area ${uniqueSuffix}`,
}

test.describe('Area Create', () => {
  let areaPage: AreaPage

  test.beforeEach(async ({ page }) => {
    areaPage = new AreaPage(page)
    await areaPage.goto()
  })

  test('should open create dialog when Add Area is clicked', async () => {
    await areaPage.openCreateDialog()
    await expect(areaPage.page.getByRole('dialog')).toBeVisible()
    await expect(areaPage.page.getByText('Add a new area to the system.')).toBeVisible()
  })

  test('should close dialog on cancel', async () => {
    await areaPage.openCreateDialog()
    await areaPage.page.keyboard.press('Escape')
    await areaPage.expectDialogClosed()
  })

  test('should show validation error when name is empty', async () => {
    await areaPage.openCreateDialog()
    await areaPage.submitCreateForm()
    await areaPage.expectFieldError('Name is required')
    await expect(areaPage.page.getByRole('dialog')).toBeVisible()
  })

  test('should show validation error when region is not selected', async () => {
    await areaPage.openCreateDialog()
    await areaPage.fillAreaForm({ name: testData.name })
    await areaPage.submitCreateForm()
    await areaPage.expectFieldError('Region is required')
    await expect(areaPage.page.getByRole('dialog')).toBeVisible()
  })

  test('should show validation error when name exceeds 100 characters', async () => {
    await areaPage.openCreateDialog()
    await areaPage.fillAreaForm({ name: 'A'.repeat(101) })
    await areaPage.submitCreateForm()
    await areaPage.expectFieldError('Name must not exceed 100 characters')
    await expect(areaPage.page.getByRole('dialog')).toBeVisible()
  })

  test('should create an area successfully', async () => {
    await areaPage.openCreateDialog()
    await areaPage.fillAreaForm({ name: testData.name })
    await areaPage.selectFirstRegion()
    await areaPage.submitCreateForm()

    await areaPage.expectSuccessToast('Area created successfully')
    await areaPage.expectDialogClosed()
    await areaPage.search(testData.name)
    await areaPage.expectRowExists(testData.name)
  })

  test('should show error when creating a duplicate area name', async () => {
    const dupName = `E2E Dup Area ${Date.now().toString(36)}`

    // First creation — should succeed
    await areaPage.openCreateDialog()
    await areaPage.fillAreaForm({ name: dupName })
    await areaPage.selectFirstRegion()
    await areaPage.submitCreateForm()
    await areaPage.expectSuccessToast('Area created successfully')
    await areaPage.expectDialogClosed()

    // Second creation with the same name — should conflict
    await areaPage.openCreateDialog()
    await areaPage.fillAreaForm({ name: dupName })
    await areaPage.selectFirstRegion()
    await areaPage.submitCreateForm()

    // Dialog stays open on conflict
    await expect(areaPage.page.getByRole('dialog')).toBeVisible()
  })

  test('newly created area should show Active status badge', async () => {
    const localName = `E2E Badge Area ${Date.now().toString(36)}`

    await areaPage.openCreateDialog()
    await areaPage.fillAreaForm({ name: localName })
    await areaPage.selectFirstRegion()
    await areaPage.submitCreateForm()
    await areaPage.expectSuccessToast()
    await areaPage.expectDialogClosed()
    await areaPage.search(localName)
    await areaPage.expectRowStatus(localName, 'Active')
  })
})
