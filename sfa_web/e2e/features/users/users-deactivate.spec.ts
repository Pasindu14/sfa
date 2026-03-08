import { test, expect } from '@playwright/test'
import { UsersPage, type UserFormData } from '../../pages/users.page'

const uniqueSuffix = Date.now().toString(36)
const uniquePhone = `+2${Date.now().toString().slice(-9)}`

const testUser: UserFormData = {
  name: `E2E Status User ${uniqueSuffix}`,
  username: `e2e_status_${uniqueSuffix}`,
  email: `e2e_status_${uniqueSuffix}@test.com`,
  phone: uniquePhone,
  role: 'SalesRep',
  deviceId: 'e2e-device-003',
  password: 'Test@1234',
}

test.describe.serial('Deactivate & Activate User', () => {
  let usersPage: UsersPage

  test('setup: create a user', async ({ page }) => {
    usersPage = new UsersPage(page)
    await usersPage.goto()

    await usersPage.openCreateDialog()
    await usersPage.fillUserForm(testUser)
    await usersPage.submitCreateForm()

    await usersPage.expectSuccessToast()
    await usersPage.expectDialogClosed()
  })

  test('should show deactivate confirmation dialog', async ({ page }) => {
    usersPage = new UsersPage(page)
    await usersPage.goto()

    await usersPage.search(testUser.username)
    await usersPage.expectRowExists(testUser.username)

    await usersPage.clickDeactivate(testUser.username)

    const alertDialog = page.getByRole('alertdialog')
    await expect(alertDialog).toBeVisible()
    await expect(alertDialog.getByText('Deactivate User')).toBeVisible()
  })

  test('should cancel deactivate and keep user active', async ({ page }) => {
    usersPage = new UsersPage(page)
    await usersPage.goto()

    await usersPage.search(testUser.username)
    await usersPage.expectRowExists(testUser.username)

    await usersPage.clickDeactivate(testUser.username)
    await usersPage.cancelAlert()

    await usersPage.expectRowStatus(testUser.username, 'Active')
  })

  test('should deactivate user successfully', async ({ page }) => {
    usersPage = new UsersPage(page)
    await usersPage.goto()

    await usersPage.search(testUser.username)
    await usersPage.expectRowExists(testUser.username)
    await usersPage.expectRowStatus(testUser.username, 'Active')

    await usersPage.clickDeactivate(testUser.username)
    await usersPage.confirmAlertAction('Deactivate')

    await usersPage.expectSuccessToast()

    // User should now show Inactive status
    await usersPage.search(testUser.username)
    await usersPage.expectRowExists(testUser.username)
    await usersPage.expectRowStatus(testUser.username, 'Inactive')
  })

  test('should show activate option for inactive user', async ({ page }) => {
    usersPage = new UsersPage(page)
    await usersPage.goto()

    await usersPage.search(testUser.username)
    await usersPage.expectRowExists(testUser.username)
    await usersPage.expectRowStatus(testUser.username, 'Inactive')

    // The dropdown should now show "Activate" instead of "Deactivate"
    await usersPage.openRowActions(testUser.username)
    await expect(page.getByRole('menuitem', { name: 'Activate' })).toBeVisible()
  })

  test('should activate user successfully', async ({ page }) => {
    usersPage = new UsersPage(page)
    await usersPage.goto()

    await usersPage.search(testUser.username)
    await usersPage.expectRowExists(testUser.username)

    await usersPage.clickActivate(testUser.username)
    await usersPage.confirmAlertAction('Activate')

    await usersPage.expectSuccessToast()

    // User should now show Active status again
    await usersPage.search(testUser.username)
    await usersPage.expectRowExists(testUser.username)
    await usersPage.expectRowStatus(testUser.username, 'Active')
  })
})
