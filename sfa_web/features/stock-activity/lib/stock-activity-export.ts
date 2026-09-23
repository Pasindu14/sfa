import { loadExcelJS } from '@/lib/utils/load-excel'
import { formatColombo } from '@/lib/utils/datetime'
import { splitCasesPieces } from '@/features/stock/lib/quantity'
import { transactionTypeLabel, type StockActivity, type StockActivityFilters } from '../schema/stock-activity.schema'
import { formatReference } from './reference'

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

const HEADERS = [
  'Date / Time',
  'User',
  'Distributor',
  'Product Code',
  'Description',
  'Stock Type',
  'Type',
  'Direction',
  'CS',
  'PCS',
  'Qty (pieces)',
  'Before',
  'After',
  'Reference',
  'Notes',
]

const WIDTHS = [18, 20, 26, 14, 36, 11, 22, 10, 8, 8, 12, 10, 10, 20, 36]

export async function exportStockActivityExcel(
  filters: StockActivityFilters,
  rows: StockActivity[],
): Promise<void> {
  const ExcelJS = await loadExcelJS()
  const wb = new ExcelJS.Workbook()
  const ws = wb.addWorksheet('Stock Activity')

  const titleRow = ws.addRow(['Stock Activity Log'])
  titleRow.font = { bold: true, size: 14 }
  ws.addRow([`${filters.from} to ${filters.to}  ·  ${rows.length} records`])
  ws.addRow([])

  const headerRow = ws.addRow(HEADERS)
  headerRow.font = { bold: true }
  headerRow.alignment = { horizontal: 'center' }

  for (const r of rows) {
    const { cases, pieces } = splitCasesPieces(r.quantity, r.piecesPerPack)
    ws.addRow([
      formatColombo(r.transactedAt, 'yyyy-MM-dd HH:mm'),
      r.transactedByName ?? '',
      r.distributorName,
      r.productCode,
      r.productDescription,
      r.stockType === 'FreeIssue' ? 'Free Issue' : 'Normal',
      transactionTypeLabel(r.transactionType),
      r.direction,
      cases,
      pieces,
      r.quantity,
      r.quantityBefore,
      r.quantityAfter,
      formatReference(r),
      r.notes ?? '',
    ])
  }

  ws.columns.forEach((col, i) => {
    col.width = WIDTHS[i] ?? 14
  })

  const buf = await wb.xlsx.writeBuffer()
  downloadBlob(
    new Blob([buf], {
      type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
    }),
    `stock-activity-${filters.from}_${filters.to}.xlsx`
  )
}
