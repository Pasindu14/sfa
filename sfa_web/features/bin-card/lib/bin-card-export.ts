import ExcelJS from 'exceljs'
import { BIN_CARD_COLUMNS } from '../components/columns/bin-card-columns'
import type { BinCardResponse } from '../schema/bin-card.schema'

function fileBase(data: BinCardResponse): string {
  const safe = data.distributorName.replace(/[^\w\-]+/g, '_')
  return `bin-card-${safe}-${data.from}_${data.to}`
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

// ── Excel ───────────────────────────────────────────────────────────────────

export async function exportBinCardExcel(data: BinCardResponse): Promise<void> {
  const wb = new ExcelJS.Workbook()
  const ws = wb.addWorksheet('Bin Card')

  const titleRow = ws.addRow([`Bin Card — ${data.distributorName}`])
  titleRow.font = { bold: true, size: 14 }
  ws.addRow([`${data.from} to ${data.to}  ·  ${data.recordCount} records`])
  ws.addRow([])

  const headerRow = ws.addRow(BIN_CARD_COLUMNS.map((c) => c.header))
  headerRow.font = { bold: true }
  headerRow.alignment = { horizontal: 'center' }

  for (const row of data.rows) {
    ws.addRow(
      BIN_CARD_COLUMNS.map((c) => {
        const v = c.get(row)
        return v === null ? '' : v
      })
    )
  }

  const totalRow = ws.addRow(
    BIN_CARD_COLUMNS.map((c, i) => {
      if (i === 0) return 'TOTAL'
      const t = c.getTotal ? c.getTotal(data.totals) : null
      return t === null || t === undefined ? '' : t
    })
  )
  totalRow.font = { bold: true }

  ws.columns.forEach((col, i) => {
    col.width = i < 2 ? 24 : 16
  })

  const buf = await wb.xlsx.writeBuffer()
  downloadBlob(
    new Blob([buf], {
      type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
    }),
    `${fileBase(data)}.xlsx`
  )
}

// ── PDF (print-to-PDF via a print window — no extra dependency) ──────────────

export function exportBinCardPdf(data: BinCardResponse): void {
  const win = window.open('', '_blank', 'width=1200,height=800')
  if (!win) return

  const esc = (s: string) =>
    s.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;')

  const fmt = (v: string | number | null, money?: boolean): string => {
    if (v === null || v === undefined) return ''
    if (typeof v === 'string') return esc(v)
    if (money) return v.toFixed(2)
    return Number.isInteger(v) ? String(v) : String(Number(v.toFixed(2)))
  }

  const head = BIN_CARD_COLUMNS.map(
    (c) => `<th style="text-align:${c.align}">${esc(c.header)}</th>`
  ).join('')

  const body = data.rows
    .map((row) => {
      const cells = BIN_CARD_COLUMNS.map((c) => {
        const v = c.get(row)
        const negative = c.key === 'stockVariance' && typeof v === 'number' && v < 0
        const style = `text-align:${c.align}${negative ? ';color:#dc2626;font-weight:600' : ''}`
        return `<td style="${style}">${fmt(v, c.money)}</td>`
      }).join('')
      return `<tr>${cells}</tr>`
    })
    .join('')

  const totals = BIN_CARD_COLUMNS.map((c, i) => {
    if (i === 0) return `<td style="text-align:left">TOTAL</td>`
    const t = c.getTotal ? c.getTotal(data.totals) : null
    return `<td style="text-align:${c.align}">${fmt(t ?? null, c.money)}</td>`
  }).join('')

  win.document.write(`<!DOCTYPE html><html><head><meta charset="utf-8" />
<title>Bin Card — ${esc(data.distributorName)}</title>
<style>
  @page { size: A4 landscape; margin: 10mm; }
  body { font-family: Arial, Helvetica, sans-serif; color:#111; margin:0; padding:16px; }
  h1 { font-size:18px; margin:0 0 2px; }
  .sub { color:#555; font-size:12px; margin:0 0 12px; }
  table { width:100%; border-collapse:collapse; font-size:10px; }
  th,td { border:1px solid #ccc; padding:3px 6px; white-space:nowrap; }
  th { background:#f0f0f0; font-size:9px; text-transform:uppercase; letter-spacing:.3px; }
  tfoot td { border-top:2px solid #888; background:#f6f6f6; font-weight:700; }
</style></head><body>
  <h1>Bin Card — ${esc(data.distributorName)}</h1>
  <p class="sub">${esc(data.from)} to ${esc(data.to)} · ${data.recordCount} records</p>
  <table>
    <thead><tr>${head}</tr></thead>
    <tbody>${body}</tbody>
    <tfoot><tr>${totals}</tr></tfoot>
  </table>
</body></html>`)
  win.document.close()
  win.focus()
  win.print()
}
