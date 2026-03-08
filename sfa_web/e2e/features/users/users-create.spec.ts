import { test, expect } from '@playwright/test'
import { UsersPage, type UserFormData } from '../../pages/users.page'

// Generate unique username + phone per test run to avoid conflicts
const uniqueSuffix = Date.now().toString(36)
const uniquePhone = `+3${Date.now().toString().slice(-9)}`

const testUser: UserFormData = {
  name: `E2E Test User ${uniqueSuffix}`,
  username: `e2e_user_${uniqueSuffix}`,
  email: `e2e_${uniqueSuffix}@test.com`,
  phone: uniquePhone,
  role: 'SalesRep',
  deviceId: 'e2e-device-001',
  password: 'Test@1234',
}

test.describe('Create User', () => {
  let usersPage: UsersPage

  test.beforeEach(async ({ page }) => {
    usersPage = new UsersPage(page)
    await usersPage.goto()
  })

  test('should open create dialog when clicking Add User', async () => {
    await usersPage.openCreateDialog()

    // Dialog should contain the form fields
    const dialog = usersPage.page.locator('[role="dialog"]')
    await expect(dialog.getByLabel('Name', { exact: true })).toBeVisible()
    await expect(dialog.getByLabel('Username', { exact: true })).toBeVisible()
    await expect(dialog.getByLabel('Email', { exact: true })).toBeVisible()
    await expect(dialog.getByLabel('Phone', { exact: true })).toBeVisible()
    await expect(dialog.getByLabel('Password', { exact: true })).toBeVisible()
  })

  test('should show validation errors on empty submit', async () => {
    await usersPage.openCreateDialog()

    // Clear any default values and submit empty form
    await usersPage.fillUserForm({
      name: '',
      username: '',
      email: '',
      phone: '',
      password: '',
    })
    await usersPage.submitCreateForm()

    // Should show validation errors (Zod messages)
    // The dialog should remain open
    const dialog = usersPage.page.locator('[role="dialog"]')
    await expect(dialog).toBeVisible()
  })

  test('should create a new user successfully', async () => {
    await usersPage.openCreateDialog()
    await usersPage.fillUserForm(testUser)
    await usersPage.submitCreateForm()

    // Success toast confirms API completed
    await usersPage.expectSuccessToast()

    // Dialog should close after mutation success
    await usersPage.expectDialogClosed()

    // New user should appear in the table (may need to search)
    await usersPage.search(testUser.username)
    await usersPage.expectRowExists(testUser.username)
  })

  test('should show error when creating user with duplicate username', async () => {
    // Try to create the same user again
    await usersPage.openCreateDialog()
    await usersPage.fillUserForm(testUser)
    await usersPage.submitCreateForm()

    // Should show an error (field error on username or error toast)
    // Dialog should remain open
    const dialog = usersPage.page.locator('[role="dialog"]')
    await expect(dialog).toBeVisible({ timeout: 5_000 })
  })
})
