import { test, expect } from '@playwright/test'
import { RoutePage, type RouteFormData } from '../../pages/route.page'

const uniqueSuffix = Date.now().toString(36) + '1'
const originalData: RouteFormData = {
  name: `E2E Update Route ${uniqueSuffix}`,
}
const updatedData: RouteFormData = {
  name: `E2E Updated Route ${uniqueSuffix}`,
}

test.describe.serial('Route Update', () => {
  let routePage: RoutePage

  test('setup: create a route to edit', async ({ page }) => {
    routePage = new RoutePage(page)
    await routePage.goto()
    await routePage.openCreateDialog()
    await routePage.fillRouteForm({ name: originalData.name })
    await routePage.selectFirstDivision()
    await routePage.submitCreateForm()
    await routePage.expectSuccessToast('Route created successfully')
    await routePage.expectDialogClosed()
    await routePage.search(originalData.name)
    await routePage.expectRowExists(originalData.name)
  })

  test('should open edit dialog with pre-filled name', async ({ page }) => {
    routePage = new RoutePage(page)
    await routePage.goto()
    await routePage.search(originalData.name)
    await routePage.clickEdit(originalData.name)

    await expect(routePage.page.getByRole('heading', { name: 'Edit Route' })).toBeVisible()
    const dialog = routePage.page.locator('[role="dialog"]')
    await expect(dialog.getByLabel('Name', { exact: true })).toHaveValue(originalData.name)
  })

  test('should show validation error when name is cleared in edit mode', async ({ page }) => {
    routePage = new RoutePage(page)
    await routePage.goto()
    await routePage.search(originalData.name)
    await routePage.clickEdit(originalData.name)

    await routePage.fillRouteForm({ name: '' })
    await routePage.submitEditForm()
    await routePage.expectFieldError('Name is required')
    await expect(routePage.page.getByRole('dialog')).toBeVisible()
  })

  test('should update route name successfully', async ({ page }) => {
    routePage = new RoutePage(page)
    await routePage.goto()
    await routePage.search(originalData.name)
    await routePage.clickEdit(originalData.name)

    await routePage.fillRouteForm({ name: updatedData.name })
    await routePage.submitEditForm()

    await routePage.expectSuccessToast('Route updated successfully')
    await routePage.expectDialogClosed()
    await routePage.search(updatedData.name)
    await routePage.expectRowExists(updatedData.name)
    await routePage.expectRowNotExists(originalData.name)
  })
})
