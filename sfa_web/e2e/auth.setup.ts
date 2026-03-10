import { test as setup } from '@playwright/test'

const ADMIN_USERNAME = process.env.E2E_ADMIN_USERNAME ?? 'admin'
const ADMIN_PASSWORD = process.env.E2E_ADMIN_PASSWORD ?? 'Admin@1234'
const AUTH_FILE = 'playwright/.auth/admin.json'

setup('authenticate as admin', async ({ page }) => {
  // Go to login page
  await page.goto('/login')

  // Fill credentials
  await page.getByLabel('Username').fill(ADMIN_USERNAME)
  await page.getByLabel('Password').fill(ADMIN_PASSWORD)

  // Submit
  await page.getByRole('button', { name: 'Login' }).click()

  // Wait for redirect after login — proves login succeeded
  await page.waitForURL('**/users', { timeout: 15_000 })

  // Save signed-in state so all tests reuse this session
  await page.context().storageState({ path: AUTH_FILE })
})
