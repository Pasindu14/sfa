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

import * as XLSX from 'xlsx'
import type {
  ImportInvoicePayload,
  ImportInvoiceItemPayload,
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

// ── Date parsing ──────────────────────────────────────────────────────────
// Handles: Excel serial number, "19-03-2026" (DD-MM-YYYY), "15-Jan-25" (DD-Mon-YY)

function parseDate(raw: unknown): string {
  if (!raw) return new Date().toISOString().split('T')[0]

  if (typeof raw === 'number') {
    const d = XLSX.SSF.parse_date_code(raw)
    return `${d.y}-${String(d.m).padStart(2, '0')}-${String(d.d).padStart(2, '0')}`
  }

  const str = String(raw).trim()

  // DD-MM-YYYY  e.g. "19-03-2026"
  const ddmmyyyy = str.match(/^(\d{1,2})-(\d{2})-(\d{4})$/)
  if (ddmmyyyy) {
    const [, d, m, y] = ddmmyyyy
    return `${y}-${m.padStart(2, '0')}-${d.padStart(2, '0')}`
  }

  // DD-Mon-YY  e.g. "15-Jan-25"
  const parsed = new Date(str)
  if (!isNaN(parsed.getTime())) return parsed.toISOString().split('T')[0]

  return str
}

function toNumber(raw: unknown): number {
  if (typeof raw === 'number') return raw
  const n = Number(String(raw ?? '').replace(/,/g, ''))
  return isNaN(n) ? 0 : n
}

function str(raw: unknown): string {
  return String(raw ?? '').trim()
}

// ── Main parse function ───────────────────────────────────────────────────

export function parseExcelFile(buffer: ArrayBuffer, fileName: string): ImportSalesInvoicesPayload {
  const workbook = XLSX.read(buffer, { type: 'array' })
  const sheet = workbook.Sheets[workbook.SheetNames[0]]
  const rows = XLSX.utils.sheet_to_json<unknown[]>(sheet, { header: 1 }) as unknown[][]

  // Skip rows 0–6 (company header, report title, date range, notes, blank, column headers)
  const dataRows = rows.slice(7)

  const invoices: ImportInvoicePayload[] = []
  let current: ImportInvoicePayload | null = null
  let lineNumber = 1

  function finalise(inv: ImportInvoicePayload) {
    inv.totalAmount = inv.items.reduce((s, i) => s + i.totalPrice, 0)
    // BUSY ERP only marks Col 5 = Y on the header row — continuation rows leave it blank.
    // If ANY item has isFreeIssue, the whole voucher is a free issue voucher → propagate to all items.
    if (inv.items.some(i => i.isFreeIssue)) {
      inv.invoiceType = 'FreeIssue'
      inv.items.forEach(i => { i.isFreeIssue = true })
    }
    invoices.push(inv)
  }

  for (const row of dataRows) {
    if (!row || row.length === 0) continue

    // Skip the "Totals" summary row at the bottom
    if (str(row[7]).toLowerCase() === 'totals' || str(row[9]).toLowerCase() === 'totals') continue

    const isHeader = row[COL.DATE] != null && str(row[COL.DATE]) !== ''

    if (isHeader) {
      if (current) finalise(current)

      const isFreeIssue = str(row[COL.FREE_ISSUE]).toUpperCase() === 'Y'
      const firstItem: ImportInvoiceItemPayload = {
        itemErpCode:     str(row[COL.ITEM_CODE]),
        itemDescription: str(row[COL.ITEM_DESC]),
        quantity:        toNumber(row[COL.QTY]),
        unit:            str(row[COL.UNIT]),
        unitPrice:       toNumber(row[COL.RATE]),
        totalPrice:      toNumber(row[COL.AMOUNT]),
        isFreeIssue,
        lineNumber:      1,
      }

      current = {
        vchBillNo:          str(row[COL.VCH_BILL_NO]),
        busyOrderRequestNo: str(row[COL.BUSY_ORDER]) || null,
        sfaPoNumber:        str(row[COL.SFA_PO]) || null,
        distributorAlias:   toNumber(row[COL.ALIAS]),
        invoiceDate:        parseDate(row[COL.DATE]),
        invoiceType:        'Regular',   // finalised after all items collected
        totalAmount:        0,           // computed in finalise()
        items:              [firstItem],
      }
      lineNumber = 2

    } else if (current) {
      const itemCode = str(row[COL.ITEM_CODE])
      if (!itemCode) continue   // truly empty continuation row

      const isFreeIssue = str(row[COL.FREE_ISSUE]).toUpperCase() === 'Y'
      current.items.push({
        itemErpCode:     itemCode,
        itemDescription: str(row[COL.ITEM_DESC]),
        quantity:        toNumber(row[COL.QTY]),
        unit:            str(row[COL.UNIT]),
        unitPrice:       toNumber(row[COL.RATE]),
        totalPrice:      toNumber(row[COL.AMOUNT]),
        isFreeIssue,
        lineNumber:      lineNumber++,
      })
    }
  }

  if (current) finalise(current)

  return { fileName, invoices }
}
