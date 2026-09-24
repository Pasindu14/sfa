import { loadExcelJS } from '@/lib/utils/load-excel'
import { formatColombo } from '@/lib/utils/datetime'
import type { DistributorStockItem } from '../schema/stock.schema'
import { splitCasesPieces } from './quantity'

function downloadBlob(blob: Blob, filename: string): void {
  const url = URL.createObjectURL(blob)
  const a = document.createElement('a')
  a.href = url
  a.download = filename
  document.body.appendChild(a)
  a.click()
  a.remove()
  URL.revokeObjectURL(url)
}

/**
 * Exports the complete stock list of a distributor (every item for the loaded filters, not one
 * table page) with balances and values at the default pricing structure's dealer prices.
 */
export async function exportStockListExcel(
  distributorName: string,
  items: DistributorStockItem[],
  filterLabel: string,
): Promise<void> {
  const ExcelJS = await loadExcelJS()
  const wb = new ExcelJS.Workbook()
  const ws = wb.addWorksheet('Stock')

  ws.addRow([`Stock Balance — ${distributorName}`]).font = { bold: true, size: 14 }
  ws.addRow([`As of ${new Date().toLocaleString()}  ·  ${filterLabel}  ·  ${items.length} items`])
  ws.addRow(['Values at the default pricing structure dealer pack price'])
  ws.addRow([])

  const header = ws.addRow([
    'Product Code', 'Description', 'Type', 'Fleet', 'CS', 'PCS', 'Total Pieces',
    'Pack Price', 'Case Price', 'Stock Value', 'Last Updated',
  ])
  header.font = { bold: true }
  header.alignment = { horizontal: 'center' }

  let totalPieces = 0
  let totalValue = 0
  for (const item of items) {
    const { cases, pieces } = splitCasesPieces(item.quantityOnHand, item.piecesPerPack)
    totalPieces += item.quantityOnHand
    totalValue += item.stockValue ?? 0
    ws.addRow([
      item.productCode,
      item.productDescription,
      item.stockType === 'FreeIssue' ? 'Free Issue' : 'Normal',
      item.fleetName ?? '',
      cases,
      pieces,
      item.quantityOnHand,
      item.dealerPackPrice ?? null,
      item.dealerCasePrice ?? null,
      item.stockValue ?? null,
      item.lastUpdatedAt ? formatColombo(item.lastUpdatedAt, 'd MMM yyyy, HH:mm') : '',
    ])
  }

  const total = ws.addRow(['TOTAL', '', '', '', '', '', totalPieces, '', '', totalValue, ''])
  total.font = { bold: true }

  for (const col of [8, 9, 10]) ws.getColumn(col).numFmt = '#,##0.00'
  ws.columns.forEach((col, i) => {
    col.width = i === 1 ? 40 : i === 0 ? 18 : i === 10 ? 20 : 13
  })

  const buf = await wb.xlsx.writeBuffer()
  const safe = distributorName.replace(/[^\w-]+/g, '_')
  downloadBlob(
    new Blob([buf], { type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet' }),
    `stock-${safe}-${new Date().toISOString().slice(0, 10)}.xlsx`,
  )
}
