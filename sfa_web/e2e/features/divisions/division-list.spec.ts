import { test, expect } from '@playwright/test'
import { DivisionPage } from '../../pages/division.page'

test.describe('Division List', () => {
  let divisionPage: DivisionPage

  test.beforeEach(async ({ page }) => {
    divisionPage = new DivisionPage(page)
    await divisionPage.goto()
  })

  test('should display the page heading', async () => {
    await expect(divisionPage.page.getByRole('heading', { name: 'Division Management' })).toBeVisible()
  })

  test('should display divisions table with correct column headers', async () => {
    await divisionPage.expectTableHasRows()
    await expect(divisionPage.page.getByRole('columnheader', { name: 'Name' })).toBeVisible()
    await expect(divisionPage.page.getByRole('columnheader', { name: 'Territory' })).toBeVisible()
    await expect(divisionPage.page.getByRole('columnheader', { name: 'Area' })).toBeVisible()
    await expect(divisionPage.page.getByRole('columnheader', { name: 'Region' })).toBeVisible()
    await expect(divisionPage.page.getByRole('columnheader', { name: 'Status' })).toBeVisible()
    await expect(divisionPage.page.getByRole('columnheader', { name: 'Created' })).toBeVisible()
    await expect(divisionPage.page.getByRole('columnheader', { name: 'Actions' })).toBeVisible()
  })

  test('should display Add Division button', async () => {
    await expect(divisionPage.addButton).toBeVisible()
  })

  test('should display search input', async () => {
    await expect(divisionPage.searchInput).toBeVisible()
  })

  test('should search divisions by name', async () => {
    await divisionPage.expectTableHasRows()

    const firstRowName = await divisionPage.table
      .locator('tbody tr')
      .first()
      .locator('span.text-sm.font-medium')
      .textContent()

    if (firstRowName) {
      await divisionPage.search(firstRowName.trim())
      await divisionPage.expectRowExists(firstRowName.trim())
    }
  })

  test('should show empty state after searching for nonexistent division', async () => {
    await divisionPage.search('zzz_nonexistent_division_xyz_99999')
    await expect(divisionPage.table.locator('tbody tr')).toHaveCount(1)
  })

  test('should restore table data after clearing search', async () => {
    await divisionPage.search('zzz_nonexistent_xyz')
    await divisionPage.clearSearch()
    await divisionPage.expectTableHasRows()
  })
})
