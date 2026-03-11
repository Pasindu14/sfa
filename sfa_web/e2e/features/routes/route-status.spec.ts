import { test, expect } from '@playwright/test'
import { RoutePage, type RouteFormData } from '../../pages/route.page'

const uniqueSuffix = Date.now().toString(36) + '2'
const testData: RouteFormData = {
  name: `E2E Status Route ${uniqueSuffix}`,
}

test.describe.serial('Route Activate / Deactivate', () => {
  let routePage: RoutePage

  test('setup: create a route to toggle status', async ({ page }) => {
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
    await routePage.expectRowStatus(testData.name, 'Active')
  })

  test('should open deactivate confirmation dialog', async ({ page }) => {
    routePage = new RoutePage(page)
    await routePage.goto()
    await routePage.search(testData.name)
    await routePage.clickDeactivate(testData.name)
    await expect(routePage.page.getByRole('alertdialog')).toBeVisible()
    await expect(routePage.page.getByRole('heading', { name: 'Deactivate Route' })).toBeVisible()
  })

  test('should cancel deactivation and keep Active status', async ({ page }) => {
    routePage = new RoutePage(page)
    await routePage.goto()
    await routePage.search(testData.name)
    await routePage.clickDeactivate(testData.name)
    await routePage.cancelAlert()
    await expect(routePage.page.getByRole('alertdialog')).not.toBeAttached({ timeout: 10_000 })
    await routePage.expectRowStatus(testData.name, 'Active')
  })

  test('should deactivate route and show Inactive badge', async ({ page }) => {
    routePage = new RoutePage(page)
    await routePage.goto()
    await routePage.search(testData.name)
    await routePage.clickDeactivate(testData.name)
    await routePage.confirmAlertAction('Deactivate')

    await routePage.expectSuccessToast('Route deactivated successfully')
    await routePage.expectRowStatus(testData.name, 'Inactive')
  })

  test('should show Activate option in menu after deactivation', async ({ page }) => {
    routePage = new RoutePage(page)
    await routePage.goto()
    await routePage.search(testData.name)
    await routePage.openRowActions(testData.name)

    await expect(routePage.page.getByRole('menuitem', { name: 'Activate', exact: true })).toBeVisible()
    await expect(routePage.page.getByRole('menuitem', { name: 'Deactivate', exact: true })).not.toBeVisible()
    // Close the menu
    await routePage.page.keyboard.press('Escape')
  })

  test('should open activate confirmation dialog', async ({ page }) => {
    routePage = new RoutePage(page)
    await routePage.goto()
    await routePage.search(testData.name)
    await routePage.clickActivate(testData.name)
    await expect(routePage.page.getByRole('alertdialog')).toBeVisible()
    await expect(routePage.page.getByRole('heading', { name: 'Activate Route' })).toBeVisible()
  })

  test('should activate route and show Active badge', async ({ page }) => {
    routePage = new RoutePage(page)
    await routePage.goto()
    await routePage.search(testData.name)
    await routePage.clickActivate(testData.name)
    await routePage.confirmAlertAction('Activate')

    await routePage.expectSuccessToast('Route activated successfully')
    await routePage.expectRowStatus(testData.name, 'Active')
  })

  test('should show Deactivate option in menu after activation', async ({ page }) => {
    routePage = new RoutePage(page)
    await routePage.goto()
    await routePage.search(testData.name)
    await routePage.openRowActions(testData.name)

    await expect(routePage.page.getByRole('menuitem', { name: 'Deactivate', exact: true })).toBeVisible()
    await expect(routePage.page.getByRole('menuitem', { name: 'Activate', exact: true })).not.toBeVisible()
    await routePage.page.keyboard.press('Escape')
  })
})
