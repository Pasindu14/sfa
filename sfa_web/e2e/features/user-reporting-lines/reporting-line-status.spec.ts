import { test, expect } from '@playwright/test'
import { UserReportingLinePage } from '../../pages/user-reporting-line.page'

let statusSubordinateName = ''

test.describe.serial('Reporting Line Status', () => {
  test.setTimeout(120_000)

  test('setup — create a reporting line for status tests', async ({ page }) => {
    const rlPage = new UserReportingLinePage(page)
    await rlPage.goto()

    const { subordinateName, managerName, managerRole } = await rlPage.getTestUsers()
    statusSubordinateName = subordinateName

    await rlPage.openCreateDialog()
    await rlPage.selectSubordinate(subordinateName)
    await rlPage.selectManagerRole(managerRole)
    await rlPage.selectManagerUser(managerName)
    await rlPage.submitForm()
    await rlPage.expectSuccessToast('Reporting line assigned successfully')
    await rlPage.expectDialogClosed()
  })

  test('new reporting line is Active by default', async ({ page }) => {
    if (!statusSubordinateName) {
      test.skip(true, 'Setup did not complete')
      return
    }
    const rlPage = new UserReportingLinePage(page)
    // Use API to bypass pagination — accumulated inactive rows push the new Active one off page 1
    await rlPage.expectApiHasActiveRecordForSubordinate(statusSubordinateName)
  })

  test('reporting line is confirmed Active before deactivation', async ({ page }) => {
    if (!statusSubordinateName) {
      test.skip(true, 'Setup did not complete')
      return
    }
    const rlPage = new UserReportingLinePage(page)
    // Use API to verify Active status (avoids pagination issues when many stale rows exist)
    await rlPage.expectApiHasActiveRecordForSubordinate(statusSubordinateName)
  })

  test('deactivates a reporting line successfully', async ({ page }) => {
    if (!statusSubordinateName) {
      test.skip(true, 'Setup did not complete')
      return
    }
    const rlPage = new UserReportingLinePage(page)
    await rlPage.goto()
    await rlPage.search(statusSubordinateName)
    await rlPage.clickDeactivate(statusSubordinateName)
    await rlPage.confirmAlertAction('Deactivate')
    await rlPage.expectSuccessToast('Reporting line deactivated')
  })

  test('reporting line shows Inactive after deactivation', async ({ page }) => {
    if (!statusSubordinateName) {
      test.skip(true, 'Setup did not complete')
      return
    }
    const rlPage = new UserReportingLinePage(page)
    await rlPage.goto()
    await rlPage.search(statusSubordinateName)
    await rlPage.expectRowStatus(statusSubordinateName, 'Inactive')
  })

  test('activates a reporting line successfully', async ({ page }) => {
    if (!statusSubordinateName) {
      test.skip(true, 'Setup did not complete')
      return
    }
    const rlPage = new UserReportingLinePage(page)
    await rlPage.goto()
    await rlPage.search(statusSubordinateName)
    await rlPage.clickActivate(statusSubordinateName)
    await rlPage.confirmAlertAction('Activate')
    await rlPage.expectSuccessToast('Reporting line activated')
  })

  test('reporting line shows Active after reactivation', async ({ page }) => {
    if (!statusSubordinateName) {
      test.skip(true, 'Setup did not complete')
      return
    }
    const rlPage = new UserReportingLinePage(page)
    await rlPage.goto()
    await rlPage.search(statusSubordinateName)
    await rlPage.expectRowStatus(statusSubordinateName, 'Active')
  })
})
