import type { BinCardRow, BinCardTotals } from '../../schema/bin-card.schema'

export type CellValue = string | number | null

export interface BinCardColumn {
  key: string
  header: string
  /** Cell value for a data row. */
  get: (row: BinCardRow) => CellValue
  /** Cell value for the totals row (omitted = blank in totals). */
  getTotal?: (t: BinCardTotals) => CellValue
  align: 'left' | 'right'
  /** Render with 2 decimal places (prices & values). */
  money?: boolean
}

/**
 * Single source of truth for the bin-card columns — consumed by the on-screen
 * table, the Excel export and the PDF export so they can never drift apart.
 * Order matches the target report layout.
 */
export const BIN_CARD_COLUMNS: BinCardColumn[] = [
  { key: 'itemCode',          header: 'Item Code',           get: (r) => r.itemCode,          align: 'left' },
  { key: 'itemDescription',   header: 'Item Description',    get: (r) => r.itemDescription,   align: 'left' },
  { key: 'itemPrice',         header: 'Item Price',          get: (r) => r.itemPrice,         align: 'right', money: true },
  { key: 'openStock',         header: 'Open Stock',          get: (r) => r.openStock,         getTotal: (t) => t.openStock,         align: 'right' },
  { key: 'invoiceQuantity',   header: 'Invoice Quantity',    get: (r) => r.invoiceQuantity,   getTotal: (t) => t.invoiceQuantity,   align: 'right' },
  { key: 'marketResaleable',  header: 'Market Resaleable',   get: (r) => r.marketResaleable,  getTotal: (t) => t.marketResaleable,  align: 'right' },
  { key: 'deletedInv',        header: 'Deleted Inv',         get: (r) => r.deletedInv,        getTotal: (t) => t.deletedInv,        align: 'right' },
  { key: 'stockAdjustment',   header: 'Stock Adjustment',    get: (r) => r.stockAdjustment,   getTotal: (t) => t.stockAdjustment,   align: 'right', money: true },
  { key: 'soldQty',           header: 'Sold Qty',            get: (r) => r.soldQty,           getTotal: (t) => t.soldQty,           align: 'right' },
  { key: 'freeIssues',        header: 'Free Issues',         get: (r) => r.freeIssues,        getTotal: (t) => t.freeIssues,        align: 'right' },
  { key: 'companyFreeIssues', header: 'Company Free Issues', get: (r) => r.companyFreeIssues, getTotal: (t) => t.companyFreeIssues, align: 'right' },
  { key: 'repReturnQtyDE',    header: 'Rep Return Qty D/E',  get: (r) => r.repReturnQtyDE,    getTotal: (t) => t.repReturnQtyDE,    align: 'right' },
  { key: 'endStock',          header: 'End Stock',           get: (r) => r.endStock,          getTotal: (t) => t.endStock,          align: 'right' },
  { key: 'currentStock',      header: 'Current Stock (Stock Taking)', get: (r) => r.currentStock,      align: 'right' },
  { key: 'closingStockValue', header: 'Closing Stock Value', get: (r) => r.closingStockValue, getTotal: (t) => t.closingStockValue, align: 'right', money: true },
  { key: 'stockVariance',     header: 'Stock Variance',      get: (r) => r.stockVariance,     align: 'right' },
]

/** Format a cell value for display (table & PDF). */
export function formatCell(v: CellValue, col: BinCardColumn): string {
  if (v === null || v === undefined) return '—'
  if (typeof v === 'string') return v
  if (col.money) return v.toFixed(2)
  return Number.isInteger(v) ? String(v) : String(Number(v.toFixed(2)))
}
