import { test, expect } from '@playwright/test'
import { UserReportingLinePage } from '../../pages/user-reporting-line.page'

let createdSubordinateName = ''

test.describe.serial('Create Reporting Line', () => {
  test.setTimeout(90_000)

  test('opens the Add reporting line dialog', async ({ page }) => {
    const rlPage = new UserReportingLinePage(page)
    await rlPage.goto()
    await rlPage.openCreateDialog()
    await expect(page.locator('[role="dialog"]')).toBeVisible()
    await expect(page.getByRole('button', { name: 'Save reporting line' })).toBeVisible()
  })

  test('creates a reporting line with API-fetched users', async ({ page }) => {
    const rlPage = new UserReportingLinePage(page)
    await rlPage.goto()

    const { subordinateName, managerName, managerRole } = await rlPage.getTestUsers()
    createdSubordinateName = subordinateName

    await rlPage.openCreateDialog()
    await rlPage.selectSubordinate(subordinateName)
    await rlPage.selectManagerRole(managerRole)
    await rlPage.selectManagerUser(managerName)
    await rlPage.submitForm()

    await rlPage.expectSuccessToast('Reporting line assigned successfully')
    await rlPage.expectDialogClosed()
  })

  test('new reporting line appears in the list', async ({ page }) => {
    if (!createdSubordinateName) {
      test.skip(true, 'No subordinate name captured — previous test may have failed')
      return
    }
    const rlPage = new UserReportingLinePage(page)
    await rlPage.goto()
    await rlPage.search(createdSubordinateName)
    await rlPage.expectRowExists(createdSubordinateName)
  })

  test('new reporting line has Active status by default', async ({ page }) => {
    if (!createdSubordinateName) {
      test.skip(true, 'No subordinate name captured')
      return
    }
    const rlPage = new UserReportingLinePage(page)
    // Use API to bypass pagination — accumulated inactive rows push the new Active one off page 1
    await rlPage.expectApiHasActiveRecordForSubordinate(createdSubordinateName)
  })
})
