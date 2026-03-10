import { test, expect } from '@playwright/test'
import { UsersPage } from '../../pages/users.page'

test.describe('User List', () => {
  let usersPage: UsersPage

  test.beforeEach(async ({ page }) => {
    usersPage = new UsersPage(page)
    await usersPage.goto()
  })

  test('should display users table with data', async () => {
    await usersPage.expectTableHasRows()

    // Verify column headers are present (exact match to avoid 'User' matching 'Username')
    await expect(usersPage.page.getByRole('columnheader', { name: 'User', exact: true })).toBeVisible()
    await expect(usersPage.page.getByRole('columnheader', { name: 'Username' })).toBeVisible()
    await expect(usersPage.page.getByRole('columnheader', { name: 'Phone' })).toBeVisible()
    await expect(usersPage.page.getByRole('columnheader', { name: 'Role' })).toBeVisible()
    await expect(usersPage.page.getByRole('columnheader', { name: 'Status' })).toBeVisible()
  })

  test('should have Add User button visible', async () => {
    await expect(usersPage.addUserButton).toBeVisible()
  })

  test('should search and filter users by text', async ({ page }) => {
    // Get the first username from the table for a known search term
    const firstUsername = await usersPage.table
      .locator('tbody tr')
      .first()
      .locator('span.font-mono')
      .textContent()

    if (firstUsername) {
      // Search for the username (without the @ prefix)
      const username = firstUsername.replace('@', '')
      await usersPage.search(username)

      // The matching row should still be visible
      await usersPage.expectRowExists(username)
    }
  })

  test('should filter users by role', async ({ page }) => {
    await usersPage.filterByRole('Admin')

    // All visible role badges should show "Admin"
    const roleBadges = usersPage.table.locator('tbody tr td:nth-child(4)')
    const count = await roleBadges.count()

    if (count > 0) {
      for (let i = 0; i < count; i++) {
        await expect(roleBadges.nth(i)).toContainText('Admin')
      }
    }

    // Reset filter
    await usersPage.filterByRole('all')
  })

  test('should show empty state or rows after clearing search', async () => {
    // Search for something unlikely
    await usersPage.search('zzz_nonexistent_user_xyz')
    await usersPage.page.waitForTimeout(1000)

    // Clear search — table should repopulate
    await usersPage.clearSearch()
    await usersPage.expectTableHasRows()
  })
})
