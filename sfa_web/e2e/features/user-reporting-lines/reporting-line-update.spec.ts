import { test, expect } from '@playwright/test'
import { UserReportingLinePage } from '../../pages/user-reporting-line.page'

let setupSubordinateName = ''

test.describe.serial('Update Reporting Line', () => {
  test.setTimeout(120_000)

  test('setup — create a reporting line to edit', async ({ page }) => {
    const rlPage = new UserReportingLinePage(page)
    await rlPage.goto()

    const { subordinateName, managerName, managerRole } = await rlPage.getTestUsers()
    setupSubordinateName = subordinateName

    await rlPage.openCreateDialog()
    await rlPage.selectSubordinate(subordinateName)
    await rlPage.selectManagerRole(managerRole)
    await rlPage.selectManagerUser(managerName)
    await rlPage.submitForm()
    await rlPage.expectSuccessToast('Reporting line assigned successfully')
    await rlPage.expectDialogClosed()
  })

  test('opens Edit Reporting Line dialog', async ({ page }) => {
    if (!setupSubordinateName) {
      test.skip(true, 'Setup did not complete')
      return
    }
    const rlPage = new UserReportingLinePage(page)
    await rlPage.goto()
    await rlPage.search(setupSubordinateName)
    await rlPage.clickEditReportingLine(setupSubordinateName)
    await expect(page.getByRole('heading', { name: 'Edit Reporting Line' })).toBeVisible()
  })

  test('updates the reporting line successfully', async ({ page }) => {
    if (!setupSubordinateName) {
      test.skip(true, 'Setup did not complete')
      return
    }
    const rlPage = new UserReportingLinePage(page)
    await rlPage.goto()
    await rlPage.search(setupSubordinateName)
    await rlPage.clickEditReportingLine(setupSubordinateName)
    await expect(page.getByRole('heading', { name: 'Edit Reporting Line' })).toBeVisible()

    // Change the effective-from date to trigger an actual update (avoids complex AsyncSelect re-selection)
    await page.locator('input[type="date"]').fill('2026-01-01')
    await rlPage.submitForm()
    await rlPage.expectSuccessToast('Reporting line updated successfully')
    await rlPage.expectDialogClosed()
  })
})
