import { test, expect } from '@playwright/test'
import { UsersPage, type UserFormData } from '../../pages/users.page'

const uniqueSuffix = Date.now().toString(36)
const uniquePhone = `+1${Date.now().toString().slice(-9)}`

const testUser: UserFormData = {
  name: `E2E Edit User ${uniqueSuffix}`,
  username: `e2e_edit_${uniqueSuffix}`,
  email: `e2e_edit_${uniqueSuffix}@test.com`,
  phone: uniquePhone,
  role: 'SalesRep',
  deviceId: 'e2e-device-002',
  password: 'Test@1234',
}

test.describe.serial('Update User', () => {
  let usersPage: UsersPage

  test('setup: create a user to edit', async ({ page }) => {
    usersPage = new UsersPage(page)
    await usersPage.goto()

    await usersPage.openCreateDialog()
    await usersPage.fillUserForm(testUser)
    await usersPage.submitCreateForm()

    await usersPage.expectSuccessToast()
    await usersPage.expectDialogClosed()
  })

  test('should open edit dialog with pre-filled data', async ({ page }) => {
    usersPage = new UsersPage(page)
    await usersPage.goto()

    await usersPage.search(testUser.username)
    await usersPage.expectRowExists(testUser.username)

    await usersPage.clickEdit(testUser.username)

    // Edit dialog should be visible with pre-filled name
    const dialog = page.locator('[role="dialog"]')
    await expect(dialog.getByRole('heading', { name: 'Edit User' })).toBeVisible()

    const nameInput = dialog.getByLabel('Name', { exact: true })
    await expect(nameInput).toHaveValue(testUser.name)

    // Password field should NOT be visible in edit mode
    await expect(dialog.getByLabel('Password', { exact: true })).toBeHidden()
  })

  test('should update user name successfully', async ({ page }) => {
    usersPage = new UsersPage(page)
    await usersPage.goto()

    await usersPage.search(testUser.username)
    await usersPage.expectRowExists(testUser.username)

    await usersPage.clickEdit(testUser.username)

    const updatedName = `Updated ${testUser.name}`
    await usersPage.fillUserForm({ name: updatedName, deviceId: testUser.deviceId })
    await usersPage.submitEditForm()

    // Success toast confirms API completed
    await usersPage.expectSuccessToast()

    // Dialog should close after mutation success
    await usersPage.expectDialogClosed()

    // Table should reflect the updated name
    await usersPage.search(testUser.username)
    const row = usersPage.getRowByUsername(testUser.username)
    await expect(row).toContainText(updatedName)
  })

  test('should show validation error when clearing required field', async ({ page }) => {
    usersPage = new UsersPage(page)
    await usersPage.goto()

    await usersPage.search(testUser.username)
    await usersPage.clickEdit(testUser.username)

    // Clear the name field
    await usersPage.fillUserForm({ name: '' })
    await usersPage.submitEditForm()

    // Dialog should remain open (validation error)
    const dialog = page.locator('[role="dialog"]')
    await expect(dialog).toBeVisible()
  })
})
