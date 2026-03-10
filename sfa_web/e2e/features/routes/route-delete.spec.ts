import { test, expect } from '@playwright/test'
import { RoutePage, type RouteFormData } from '../../pages/route.page'

const uniqueSuffix = Date.now().toString(36) + '2'
const testData: RouteFormData = {
  name: `E2E Delete Route ${uniqueSuffix}`,
}

test.describe.serial('Route Delete', () => {
  let routePage: RoutePage

  test('setup: create a route to delete', async ({ page }) => {
    routePage = new RoutePage(page)
    await routePage.goto()
    await routePage.openCreateDialog()
    await routePage.fillRouteForm({ name: testData.name })
    await routePage.selectFirstDivision()
    await routePage.submitCreateForm()
    await routePage.expectSuccessToast('Route created successfully')
    await routePage.expectDialogClosed()
    await routePage.search(testData.name)
    await routePage.expectRowExists(testData.name)
  })

  test('should open delete confirmation dialog', async ({ page }) => {
    routePage = new RoutePage(page)
    await routePage.goto()
    await routePage.search(testData.name)
    await routePage.clickDelete(testData.name)

    await expect(routePage.page.getByRole('alertdialog')).toBeVisible()
    await expect(routePage.page.getByRole('heading', { name: 'Delete Route' })).toBeVisible()
    await expect(
      routePage.page.getByText('This action cannot be undone. The route will be permanently removed.')
    ).toBeVisible()
  })

  test('should cancel deletion and keep route in table', async ({ page }) => {
    routePage = new RoutePage(page)
    await routePage.goto()
    await routePage.search(testData.name)
    await routePage.clickDelete(testData.name)
    await routePage.cancelAlert()

    await expect(routePage.page.getByRole('alertdialog')).not.toBeAttached({ timeout: 10_000 })
    await routePage.expectRowExists(testData.name)
  })

  test('should delete route and remove from table', async ({ page }) => {
    routePage = new RoutePage(page)
    await routePage.goto()
    await routePage.search(testData.name)
    await routePage.clickDelete(testData.name)
    await routePage.confirmAlertAction('Delete')

    await routePage.expectSuccessToast('Route deleted successfully')
    await routePage.page.waitForTimeout(500)
    await routePage.expectRowNotExists(testData.name)
  })
})
