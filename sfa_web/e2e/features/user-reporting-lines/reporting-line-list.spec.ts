import { test, expect } from '@playwright/test'
import { UserReportingLinePage } from '../../pages/user-reporting-line.page'

test.describe('User Reporting Line List', () => {
  test('shows the User Assignments heading', async ({ page }) => {
    const rlPage = new UserReportingLinePage(page)
    await rlPage.goto()
    await expect(page.getByRole('heading', { name: 'User Assignments' })).toBeVisible()
  })

  test('shows the reporting lines table with data', async ({ page }) => {
    const rlPage = new UserReportingLinePage(page)
    await rlPage.goto()
    await rlPage.expectTableHasRows()
  })

  test('shows expected column headers', async ({ page }) => {
    const rlPage = new UserReportingLinePage(page)
    await rlPage.goto()
    await expect(page.getByRole('columnheader', { name: 'Subordinate' })).toBeVisible()
    await expect(page.getByRole('columnheader', { name: 'Role', exact: true })).toBeVisible()
    await expect(page.getByRole('columnheader', { name: 'Reports To' })).toBeVisible()
    await expect(page.getByRole('columnheader', { name: 'Manager Role' })).toBeVisible()
    await expect(page.getByRole('columnheader', { name: 'Status' })).toBeVisible()
  })

  test('shows Add Reporting Line button', async ({ page }) => {
    const rlPage = new UserReportingLinePage(page)
    await rlPage.goto()
    await expect(rlPage.addButton).toBeVisible()
  })

  test('shows search input', async ({ page }) => {
    const rlPage = new UserReportingLinePage(page)
    await rlPage.goto()
    await expect(rlPage.searchInput).toBeVisible()
  })

  test('each row has an actions menu', async ({ page }) => {
    const rlPage = new UserReportingLinePage(page)
    await rlPage.goto()
    await expect(
      rlPage.table.locator('tbody tr').first().getByRole('button', { name: 'Open menu' })
    ).toBeVisible()
  })
})
