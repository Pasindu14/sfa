import * as XLSX from 'xlsx'
import type { ImportSalesTargetsPayload, TargetRowRequest } from '../schema/sales-target.schema'

// Excel layout (0-indexed):
//   Row 6  → col 0 = Year (e.g. 2026), col 5 = Month number (e.g. 4)
//   Row 7  → column headers
//   Row 8+ → data rows
//   Data cols: 6=RepsCode, 9=ItemCode, 11=TargetQty

const MONTH_NAMES: Record<string, number> = {
  january: 1, february: 2, march: 3, april: 4,
  may: 5, june: 6, july: 7, august: 8,
  september: 9, october: 10, november: 11, december: 12,
}

export function parseTargetsExcel(
  buffer: ArrayBuffer,
  fileName: string,
): ImportSalesTargetsPayload {
  const wb = XLSX.read(buffer, { type: 'array' })
  const ws = wb.Sheets[wb.SheetNames[0]]
  const aoa: unknown[][] = XLSX.utils.sheet_to_json(ws, { header: 1, defval: '' })

  // Row 6 — period header
  const periodRow = (aoa[6] ?? []) as unknown[]
  const year = typeof periodRow[0] === 'number'
    ? periodRow[0]
    : parseInt(String(periodRow[0]), 10)
  // col 5 can be a number or month-name text
  let month: number
  const monthRaw = periodRow[5]
  if (typeof monthRaw === 'number') {
    month = monthRaw
  } else {
    const key = String(monthRaw).toLowerCase().trim()
    month = MONTH_NAMES[key] ?? 0
  }

  if (!year || !month || year < 2020 || month < 1 || month > 12) {
    throw new Error(`Could not read year/month from row 7 (got year=${year}, month=${monthRaw})`)
  }

  const rows: TargetRowRequest[] = []
  for (let i = 8; i < aoa.length; i++) {
    const row = aoa[i] as unknown[]
    const repsCode = parseInt(String(row[6] ?? ''), 10)
    const itemCode = String(row[9] ?? '').trim()
    const targetQty = parseFloat(String(row[11] ?? '0'))

    if (!repsCode || !itemCode) continue   // blank / separator rows
    rows.push({ rowIndex: i - 7, repsCode, itemCode, targetQty: isNaN(targetQty) ? 0 : targetQty })
  }

  return { fileName, year, month, rows }
}
