import { loadExcelJS } from '@/lib/utils/load-excel'
import type { DistributorStockItem } from '@/features/stock/schema/stock.schema'
import { splitCasesPieces } from '@/features/stock/lib/quantity'

function fileBase(distributorName: string): string {
  const safe = distributorName.replace(/[^\w\-]+/g, '_')
  const date = new Date().toISOString().slice(0, 10)
  return `stock-${safe}-${date}`
}

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

export async function exportDistributorStockExcel(
  distributorName: string,
  items: DistributorStockItem[],
): Promise<void> {
  const ExcelJS = await loadExcelJS()
  const wb = new ExcelJS.Workbook()
  const ws = wb.addWorksheet('Stock')

  const titleRow = ws.addRow([`Remaining Stock — ${distributorName}`])
  titleRow.font = { bold: true, size: 14 }
  ws.addRow([`As of ${new Date().toLocaleString()}  ·  ${items.length} lines`])
  ws.addRow([])

  const headerRow = ws.addRow(['Product Code', 'Description', 'Type', 'CS', 'PCS', 'Total Pieces'])
  headerRow.font = { bold: true }
  headerRow.alignment = { horizontal: 'center' }

  let total = 0
  for (const item of items) {
    const { cases, pieces } = splitCasesPieces(item.quantityOnHand, item.piecesPerPack)
    total += item.quantityOnHand
    ws.addRow([
      item.productCode,
      item.productDescription,
      item.stockType === 'FreeIssue' ? 'Free Issue' : 'Normal',
      cases,
      pieces,
      item.quantityOnHand,
    ])
  }

  const totalRow = ws.addRow(['TOTAL', '', '', '', '', total])
  totalRow.font = { bold: true }

  ws.columns.forEach((col, i) => {
    col.width = i === 1 ? 40 : i === 0 ? 18 : 14
  })

  const buf = await wb.xlsx.writeBuffer()
  downloadBlob(
    new Blob([buf], {
      type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
    }),
    `${fileBase(distributorName)}.xlsx`
  )
}
