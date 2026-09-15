// Client-side Excel parsing utility — no 'use server'.
// Column layout verified against actual BUSY ERP export:
//
//  Col 0  spacer (blank)
//  Col 1  Date              — header rows only
//  Col 2  SFA PO            — header rows only
//  Col 3  BUSY Order Req    — header rows only
//  Col 4  Vch/Bill No       — header rows only (idempotency key)
//  Col 5  Free Issue        — "Y" or blank
//  Col 6  Alias             — distributor numeric code
//  Col 7  Particulars       — party name (display only)
//  Col 8  Item Alias        — ERP product code  e.g. CF01
//  Col 9  Item Details      — product description
//  Col 10 Qty.
//  Col 11 Gross             — unit price (same as Price for regular items)
//  Col 12 Unit              — e.g. Case
//  Col 13 Price             — selling unit price
//  Col 14 Amount            — line total  (Qty × Price)
//
// Invoice total is computed as sum of line items — no dedicated column.
//
// BUSY repeats the column-label row ("Date | Vch/Bill No | … | Amount") before every
// voucher block after the first, so a row is only treated as the start of an invoice
// when its Date cell holds a *parseable date* — never merely a non-empty cell. Parsing
// starts after the first such label row rather than at a fixed offset.

import * as XLSX from 'xlsx'
import type {
  ImportInvoicePayload,
  ImportSalesInvoicesPayload,
} from '../schema/sales-invoice.schema'

const COL = {
  DATE:        1,
  SFA_PO:      2,
  BUSY_ORDER:  3,
  VCH_BILL_NO: 4,
  FREE_ISSUE:  5,
  ALIAS:       6,
  // Col 7 = Particulars / party name — not needed for import
  ITEM_CODE:   8,
  ITEM_DESC:   9,
  QTY:         10,
  // Col 11 = Gross (unit price pre-discount) — not used
  UNIT:        12,
  RATE:        13,  // Price (actual selling price)
  AMOUNT:      14,  // Line total
} as const

// The exact column-label row the parser is built against. Everything is read by position,
// so a file whose labels differ (a column added, removed or reordered in the BUSY report)
// would silently put prices into amounts, units into prices, etc. Rejected up front instead.
const EXPECTED_HEADERS: ReadonlyArray<readonly [index: number, label: string]> = [
  [COL.DATE, 'Date'],
  [COL.SFA_PO, 'SFA PO'],
  [COL.BUSY_ORDER, 'BUSY PO'],
  [COL.VCH_BILL_NO, 'Vch/Bill No'],
  [COL.FREE_ISSUE, 'Free Issue'],
  [COL.ALIAS, 'Alias'],
  [7, 'Particulars'],
  [COL.ITEM_CODE, 'Item Alias'],
  [COL.ITEM_DESC, 'Item Details'],
  [COL.QTY, 'Qty.'],
  [11, 'Gross'],
  [COL.UNIT, 'Unit'],
  [COL.RATE, 'Price'],
  [COL.AMOUNT, 'Amount'],
]

/** A row the parser could not turn into an invoice, tagged with its Excel row number. */
export interface ParseIssue {
  /** 1-based Excel row, so the user can jump straight to it in the sheet. */
  row: number
  vchBillNo: string | null
  message: string
}

export interface ParseResult {
  payload: ImportSalesInvoicesPayload
  issues: ParseIssue[]
}

// ── Date parsing ──────────────────────────────────────────────────────────
// Handles: Excel serial number, "19-03-2026" (DD-MM-YYYY), "15-Jan-25" (DD-Mon-YY).
// Returns null for anything else — an unreadable date must surface as an issue, never
// be guessed at or passed through as raw text (the API rejects it with a JSON error
// that means nothing to the person doing the import).

const DD_MM_YYYY = /^(\d{1,2})-(\d{1,2})-(\d{4})$/
const DD_MON_YY = /^\d{1,2}[-/ ][A-Za-z]{3,9}[-/ ]\d{2,4}$/

function parseDate(raw: unknown): string | null {
  if (raw == null) return null

  if (typeof raw === 'number') {
    const d = XLSX.SSF.parse_date_code(raw)
    if (!d || !d.y) return null
    return `${d.y}-${String(d.m).padStart(2, '0')}-${String(d.d).padStart(2, '0')}`
  }

  if (raw instanceof Date) {
    return isNaN(raw.getTime()) ? null : toIsoDate(raw)
  }

  const s = String(raw).trim()
  if (!s) return null

  // DD-MM-YYYY  e.g. "19-03-2026"
  const ddmmyyyy = s.match(DD_MM_YYYY)
  if (ddmmyyyy) {
    const [, d, m, y] = ddmmyyyy
    const month = Number(m)
    const day = Number(d)
    if (month < 1 || month > 12 || day < 1 || day > 31) return null
    return `${y}-${m.padStart(2, '0')}-${d.padStart(2, '0')}`
  }

  // DD-Mon-YY  e.g. "15-Jan-25". Pattern-gated: bare `new Date(str)` happily turns
  // stray words into dates, which is how header text became an invoice date.
  if (DD_MON_YY.test(s)) {
    const parsed = new Date(s)
    if (!isNaN(parsed.getTime())) return toIsoDate(parsed)
  }

  return null
}

function toIsoDate(d: Date): string {
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`
}

function toNumber(raw: unknown): number {
  if (typeof raw === 'number') return raw
  const n = Number(String(raw ?? '').replace(/,/g, ''))
  return isNaN(n) ? 0 : n
}

function str(raw: unknown): string {
  return String(raw ?? '').trim()
}

/** Strict numeric read for line values — null for blank or non-numeric, never a silent 0. */
function parseNumber(raw: unknown): number | null {
  if (typeof raw === 'number') return Number.isFinite(raw) ? raw : null
  const s = str(raw).replace(/,/g, '')
  if (!s) return null
  const n = Number(s)
  return Number.isFinite(n) ? n : null
}

function normaliseLabel(raw: unknown): string {
  return str(raw).toLowerCase().replace(/\s+/g, ' ').replace(/\.$/, '')
}

/** Every expected label must sit in its expected column; returns one message per mismatch. */
function findHeaderMismatches(row: unknown[]): string[] {
  const mismatches: string[] = []
  for (const [index, label] of EXPECTED_HEADERS) {
    const found = str(row[index])
    if (normaliseLabel(found) === normaliseLabel(label)) continue
    const where = `column ${XLSX.utils.encode_col(index)}`
    const actualIndex = row.findIndex(cell => normaliseLabel(cell) === normaliseLabel(label))
    mismatches.push(
      actualIndex === -1
        ? `'${label}' is missing (expected in ${where}${found ? `, found '${found}'` : ''})`
        : `'${label}' is in column ${XLSX.utils.encode_col(actualIndex)}, expected ${where}`,
    )
  }
  return mismatches
}

// ── Row classification ────────────────────────────────────────────────────

/** The column-label row BUSY repeats before each voucher block — structure, not an error. */
function isColumnLabelRow(row: unknown[]): boolean {
  const date = str(row[COL.DATE]).toLowerCase()
  const vch = str(row[COL.VCH_BILL_NO]).toLowerCase()
  const item = str(row[COL.ITEM_CODE]).toLowerCase()
  return date === 'date' || vch === 'vch/bill no' || item === 'item alias'
}

function isTotalsRow(row: unknown[]): boolean {
  return str(row[7]).toLowerCase() === 'totals' || str(row[9]).toLowerCase() === 'totals'
}

// ── Main parse function ───────────────────────────────────────────────────

export function parseExcelFile(buffer: ArrayBuffer, fileName: string): ParseResult {
  const workbook = XLSX.read(buffer, { type: 'array' })
  const sheetName = workbook.SheetNames[0]
  const sheet = sheetName ? workbook.Sheets[sheetName] : undefined
  if (!sheet) {
    throw new Error('The workbook has no sheets.')
  }

  const rows = XLSX.utils.sheet_to_json<unknown[]>(sheet, { header: 1 }) as unknown[][]

  // Excel row number of rows[0]: sheet_to_json is relative to the used range, which does
  // not always start at A1 (leading blank rows shift it).
  const firstSheetRow = XLSX.utils.decode_range(sheet['!ref'] ?? 'A1').s.r + 1

  // Anchor on the column-label row rather than a fixed "skip 7" — the preamble block
  // (company name, voucher series, date range) varies in height between exports.
  // Anchored on the labels themselves (any position), so a shifted layout is still found and
  // reported precisely rather than as a generic "header not found".
  const labelRowIndex = rows.findIndex(
    row =>
      Array.isArray(row) &&
      row.some(cell => normaliseLabel(cell) === 'vch/bill no') &&
      row.some(cell => normaliseLabel(cell) === 'item alias'),
  )
  if (labelRowIndex === -1) {
    throw new Error(
      "couldn't find the column header row — expected a row containing 'Vch/Bill No' and 'Item Alias'. Is this a BUSY ERP sales voucher export?",
    )
  }

  const headerMismatches = findHeaderMismatches(rows[labelRowIndex])
  if (headerMismatches.length > 0) {
    throw new Error(
      `the columns don't match the BUSY sales voucher layout (header row ${firstSheetRow + labelRowIndex}): ` +
        headerMismatches.join('; ') +
        '. Expected columns B–O: ' +
        EXPECTED_HEADERS.map(([, label]) => label).join(', ') +
        '.',
    )
  }

  const startIndex = labelRowIndex + 1
  const dataRows = rows.slice(startIndex)

  const invoices: ImportInvoicePayload[] = []
  const issues: ParseIssue[] = []
  // Vch/Bill No -> the row it first appeared on, so a repeat can point back to it.
  // The API also rejects duplicates already in the database, but a dupe *within this
  // file* would otherwise silently overwrite nothing — both rows would be sent to the
  // API and one would come back as an opaque "Already imported" skip after the fact.
  const seenVchBillNos = new Map<string, number>()
  let current: ImportInvoicePayload | null = null
  let currentRow = 0
  let lineNumber = 1
  // Line-level problems for the voucher being built; any one of them drops the whole voucher
  // (a partial invoice would import with the wrong total).
  let lineProblems: string[] = []

  function addItem(inv: ImportInvoicePayload, row: unknown[], sheetRow: number, itemErpCode: string) {
    const quantity = parseNumber(row[COL.QTY])
    const unitPrice = parseNumber(row[COL.RATE])
    const totalPrice = parseNumber(row[COL.AMOUNT])
    const itemDescription = str(row[COL.ITEM_DESC])
    const unit = str(row[COL.UNIT])

    const problems: string[] = []
    if (quantity === null) problems.push(`Qty. "${str(row[COL.QTY])}" is not a number`)
    if (unitPrice === null) problems.push(`Price "${str(row[COL.RATE])}" is not a number`)
    if (totalPrice === null) problems.push(`Amount "${str(row[COL.AMOUNT])}" is not a number`)
    if (!itemDescription) problems.push('Item Details is empty')
    if (!unit) problems.push('Unit is empty')
    if (problems.length > 0) {
      lineProblems.push(`row ${sheetRow} (${itemErpCode}): ${problems.join(', ')}`)
      return
    }

    inv.items.push({
      itemErpCode,
      itemDescription,
      quantity: quantity!,
      unit,
      unitPrice: unitPrice!,
      totalPrice: totalPrice!,
      isFreeIssue: str(row[COL.FREE_ISSUE]).toUpperCase() === 'Y',
      lineNumber: lineNumber++,
    })
  }

  function finalise(inv: ImportInvoicePayload, headerRow: number) {
    if (lineProblems.length > 0) {
      issues.push({
        row: headerRow,
        vchBillNo: inv.vchBillNo,
        message: `invalid line values — ${lineProblems.join('; ')}`,
      })
      return
    }
    if (inv.items.length === 0) {
      issues.push({
        row: headerRow,
        vchBillNo: inv.vchBillNo,
        message: 'no line items were found for this voucher',
      })
      return
    }
    inv.totalAmount = inv.items.reduce((s, i) => s + i.totalPrice, 0)
    // BUSY ERP only marks Col 5 = Y on the header row — continuation rows leave it blank.
    // If ANY item has isFreeIssue, the whole voucher is a free issue voucher → propagate to all items.
    if (inv.items.some(i => i.isFreeIssue)) {
      inv.invoiceType = 'FreeIssue'
      inv.items.forEach(i => { i.isFreeIssue = true })
    }
    invoices.push(inv)
  }

  for (let i = 0; i < dataRows.length; i++) {
    const row = dataRows[i]
    const sheetRow = firstSheetRow + startIndex + i

    if (!row || row.length === 0) continue
    if (isTotalsRow(row)) continue
    if (isColumnLabelRow(row)) continue

    const dateCell = row[COL.DATE]
    const startsInvoice = str(dateCell) !== ''

    if (startsInvoice) {
      if (current) finalise(current, currentRow)
      // Cleared up front: if this header turns out to be unusable, the block's
      // continuation rows must be dropped too, not appended to the previous invoice.
      current = null

      const invoiceDate = parseDate(dateCell)
      const vchBillNo = str(row[COL.VCH_BILL_NO])
      const distributorAlias = toNumber(row[COL.ALIAS])

      const problems: string[] = []
      if (!invoiceDate) problems.push(`unreadable date "${str(dateCell)}"`)
      if (!vchBillNo) problems.push('missing voucher number')
      if (!distributorAlias) problems.push(`missing or non-numeric distributor alias "${str(row[COL.ALIAS])}"`)
      if (vchBillNo && seenVchBillNos.has(vchBillNo)) {
        problems.push(`duplicate voucher number — already used at row ${seenVchBillNos.get(vchBillNo)} in this file`)
      }

      if (problems.length > 0) {
        issues.push({
          row: sheetRow,
          vchBillNo: vchBillNo || null,
          message: problems.join(', '),
        })
        continue
      }

      seenVchBillNos.set(vchBillNo, sheetRow)

      const itemErpCode = str(row[COL.ITEM_CODE])

      current = {
        vchBillNo,
        busyOrderRequestNo: str(row[COL.BUSY_ORDER]) || null,
        sfaPoNumber:        str(row[COL.SFA_PO]) || null,
        distributorAlias,
        invoiceDate:        invoiceDate!,
        invoiceType:        'Regular',   // finalised after all items collected
        totalAmount:        0,           // computed in finalise()
        items:              [],
      }
      currentRow = sheetRow
      lineNumber = 1
      lineProblems = []

      // A voucher header normally carries its first line item on the same row.
      if (itemErpCode) addItem(current, row, sheetRow, itemErpCode)

    } else if (current) {
      const itemErpCode = str(row[COL.ITEM_CODE])
      if (!itemErpCode) continue   // truly empty continuation row

      addItem(current, row, sheetRow, itemErpCode)
    }
  }

  if (current) finalise(current, currentRow)

  return { payload: { fileName, invoices }, issues }
}
