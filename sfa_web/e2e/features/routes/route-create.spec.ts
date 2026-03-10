import { test, expect } from '@playwright/test'
import { RoutePage, type RouteFormData } from '../../pages/route.page'

const uniqueSuffix = Date.now().toString(36)
const testData: RouteFormData = {
  name: `E2E Route ${uniqueSuffix}`,
  pinColor: '#e11d48',
}

test.describe('Route Create', () => {
  let routePage: RoutePage

  test.beforeEach(async ({ page }) => {
    routePage = new RoutePage(page)
    await routePage.goto()
  })

  test('should open create dialog when Add Route is clicked', async () => {
    await routePage.openCreateDialog()
    await expect(routePage.page.getByRole('dialog')).toBeVisible()
    await expect(routePage.page.getByText('Add a new route to the system.')).toBeVisible()
  })

  test('should close dialog on cancel', async () => {
    await routePage.openCreateDialog()
    await routePage.page.keyboard.press('Escape')
    await routePage.expectDialogClosed()
  })

  test('should show validation error when name is empty', async () => {
    await routePage.openCreateDialog()
    await routePage.submitCreateForm()
    await routePage.expectFieldError('Name is required')
    await expect(routePage.page.getByRole('dialog')).toBeVisible()
  })

  test('should show validation error when name exceeds 100 characters', async () => {
    await routePage.openCreateDialog()
    await routePage.fillRouteForm({ name: 'A'.repeat(101) })
    await routePage.submitCreateForm()
    await routePage.expectFieldError('Name must not exceed 100 characters')
    await expect(routePage.page.getByRole('dialog')).toBeVisible()
  })

  test('should show validation error when division is not selected', async () => {
    await routePage.openCreateDialog()
    await routePage.fillRouteForm({ name: testData.name })
    await routePage.submitCreateForm()
    await routePage.expectFieldError('Division is required')
    await expect(routePage.page.getByRole('dialog')).toBeVisible()
  })

  test('should create a route successfully', async () => {
    await routePage.openCreateDialog()
    await routePage.fillRouteForm({ name: testData.name })
    await routePage.selectFirstDivision()
    await routePage.submitCreateForm()

    await routePage.expectSuccessToast('Route created successfully')
    await routePage.expectDialogClosed()
    await routePage.search(testData.name)
    await routePage.expectRowExists(testData.name)
  })

  test('should create a route with description', async () => {
    const nameWithDesc = `E2E Route Desc ${uniqueSuffix}`
    await routePage.openCreateDialog()
    await routePage.fillRouteForm({
      name: nameWithDesc,
      description: 'A test route description',
    })
    await routePage.selectFirstDivision()
    await routePage.submitCreateForm()

    await routePage.expectSuccessToast('Route created successfully')
    await routePage.expectDialogClosed()
    await routePage.search(nameWithDesc)
    await routePage.expectRowExists(nameWithDesc)
  })

  test('should show error when creating a duplicate route name', async () => {
    const dupName = `E2E Dup Route ${Date.now().toString(36)}`

    // First creation — should succeed
    await routePage.openCreateDialog()
    await routePage.fillRouteForm({ name: dupName })
    await routePage.selectFirstDivision()
    await routePage.submitCreateForm()
    await routePage.expectSuccessToast('Route created successfully')
    await routePage.expectDialogClosed()

    // Second creation with the same name — should conflict
    await routePage.openCreateDialog()
    await routePage.fillRouteForm({ name: dupName })
    await routePage.selectFirstDivision()
    await routePage.submitCreateForm()

    // Dialog stays open on conflict
    await expect(routePage.page.getByRole('dialog')).toBeVisible()
  })
})
