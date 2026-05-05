import * as XLSX from 'xlsx'
import type { ImportSalesTargetsPayload } from '../schema/sales-target.schema'

// Excel layout (0-indexed):
//   Row 6  → col 0 = Year (e.g. 2026), col 5 = Month number (e.g. 4)
//   Row 7  → column headers
//   Row 8+ → data rows
//   Data cols: 6=RepsCode, 7=RepName, 9=ItemCode, 10=ItemName, 11=TargetQty

const MONTH_NAMES: Record<string, number> = {
  january: 1, february: 2, march: 3, april: 4,
  may: 5, june: 6, july: 7, august: 8,
  september: 9, october: 10, november: 11, december: 12,
}

export type ParsedTargetRow = {
  rowIndex: number
  repsCode: number
  repName: string
  itemCode: string
  itemName: string
  targetQty: number
}

export type ParsedTargetsData = {
  fileName: string
  year: number
  month: number
  rows: ParsedTargetRow[]
}

export function toApiPayload(data: ParsedTargetsData): ImportSalesTargetsPayload {
  return {
    fileName: data.fileName,
    year: data.year,
    month: data.month,
    rows: data.rows.map((r) => ({
      rowIndex: r.rowIndex,
      repsCode: r.repsCode,
      itemCode: r.itemCode,
      targetQty: r.targetQty,
    })),
  }
}

export function parseTargetsExcel(
  buffer: ArrayBuffer,
  fileName: string,
): ParsedTargetsData {
  const wb = XLSX.read(buffer, { type: 'array' })
  const ws = wb.Sheets[wb.SheetNames[0]]
  const aoa: unknown[][] = XLSX.utils.sheet_to_json(ws, { header: 1, defval: '' })

  const periodRow = (aoa[6] ?? []) as unknown[]
  const year = typeof periodRow[0] === 'number'
    ? periodRow[0]
    : parseInt(String(periodRow[0]), 10)

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

  const rows: ParsedTargetRow[] = []
  for (let i = 8; i < aoa.length; i++) {
    const row = aoa[i] as unknown[]
    const repsCode = parseInt(String(row[6] ?? ''), 10)
    const repName = String(row[7] ?? '').trim()
    const itemCode = String(row[9] ?? '').trim()
    const itemName = String(row[10] ?? '').trim()
    const targetQty = parseFloat(String(row[11] ?? '0'))

    if (!repsCode || !itemCode) continue
    rows.push({
      rowIndex: i - 7,
      repsCode,
      repName,
      itemCode,
      itemName,
      targetQty: isNaN(targetQty) ? 0 : targetQty,
    })
  }

  return { fileName, year, month, rows }
}
