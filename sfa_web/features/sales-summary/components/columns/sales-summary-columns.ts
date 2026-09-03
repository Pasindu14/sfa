import type {
  SalesSummaryGroupBy,
  SalesSummaryRow,
  SalesSummaryTotals,
} from '../../schema/sales-summary.schema'

export type CellValue = string | number | null

export interface SalesSummaryColumn {
  key: string
  header: string
  /** Cell value for a data row. */
  get: (row: SalesSummaryRow) => CellValue
  /** Cell value for the totals row (omitted = blank in totals). */
  getTotal?: (t: SalesSummaryTotals) => CellValue
  align: 'left' | 'right'
  /** Render with 2 decimal places (money and percentages). */
  money?: boolean
}

/** The label column's header follows the grouping the user picked. */
const GROUP_HEADER: Record<SalesSummaryGroupBy, string> = {
  SalesRep: 'Sales Rep',
  Supervisor: 'Supervisor',
  Asm: 'ASM',
  Rsm: 'RSM',
  Nsm: 'NSM',
  Distributor: 'Distributor',
  Outlet: 'Outlet',
  Route: 'Route',
  Division: 'Division',
  Territory: 'Territory',
  Area: 'Area',
  Region: 'Region',
  Product: 'Product',
}

/**
 * Single source of truth for the report's columns — consumed by the on-screen table, the Excel
 * export and the PDF export so those three can never drift apart.
 *
 * Column order follows the report specification. Note there is deliberately no "Date Range"
 * column: the range is a filter, and it appears once in the report header and the export title.
 *
 * Two intentional asymmetries carried over from the spec — Gross Sale Value is already net of
 * Good Returns, and Net Sale Qty deducts Good Return Qty only (damage/expiry stock never
 * re-enters saleable inventory, so its quantity is never subtracted).
 */
export function buildSalesSummaryColumns(groupBy: SalesSummaryGroupBy): SalesSummaryColumn[] {
  const isProduct = groupBy === 'Product'

  return [
    ...(isProduct
      ? [
          {
            key: 'groupCode',
            header: 'Item Code',
            get: (r: SalesSummaryRow) => r.groupCode,
            align: 'left' as const,
          },
        ]
      : []),
    {
      key: 'groupName',
      header: GROUP_HEADER[groupBy],
      get: (r) => r.groupName,
      align: 'left',
    },
    { key: 'targetValue',     header: 'Target Value',      get: (r) => r.targetValue,     getTotal: (t) => t.targetValue,     align: 'right', money: true },
    { key: 'targetQty',       header: 'Target Qty',        get: (r) => r.targetQty,       getTotal: (t) => t.targetQty,       align: 'right' },
    { key: 'grossSaleValue',  header: 'Gross Sale Value',  get: (r) => r.grossSaleValue,  getTotal: (t) => t.grossSaleValue,  align: 'right', money: true },
    { key: 'saleQty',         header: 'Sale Qty',          get: (r) => r.saleQty,         getTotal: (t) => t.saleQty,         align: 'right' },
    { key: 'goodReturn',      header: 'Good Return',       get: (r) => r.goodReturn,      getTotal: (t) => t.goodReturn,      align: 'right', money: true },
    { key: 'goodReturnQty',   header: 'Good Return Qty',   get: (r) => r.goodReturnQty,   getTotal: (t) => t.goodReturnQty,   align: 'right' },
    { key: 'marketReturn',    header: 'Market Return',     get: (r) => r.marketReturn,    getTotal: (t) => t.marketReturn,    align: 'right', money: true },
    { key: 'marketReturnQty', header: 'Market Return Qty', get: (r) => r.marketReturnQty, getTotal: (t) => t.marketReturnQty, align: 'right' },
    { key: 'dbDiscount',      header: 'DB Discount',       get: (r) => r.dbDiscount,      getTotal: (t) => t.dbDiscount,      align: 'right', money: true },
    { key: 'discount',        header: 'Discount',          get: (r) => r.discount,        getTotal: (t) => t.discount,        align: 'right', money: true },
    { key: 'netSaleValue',    header: 'Net Sale Value',    get: (r) => r.netSaleValue,    getTotal: (t) => t.netSaleValue,    align: 'right', money: true },
    { key: 'netSaleQty',      header: 'Net Sale Qty',      get: (r) => r.netSaleQty,      getTotal: (t) => t.netSaleQty,      align: 'right' },
    { key: 'achievementPercent', header: 'Achieved %',     get: (r) => r.achievementPercent, getTotal: (t) => t.achievementPercent, align: 'right', money: true },
  ]
}

/** Format a cell for display (table and PDF). A dash means "not measurable", not zero. */
export function formatCell(v: CellValue, col: SalesSummaryColumn): string {
  if (v === null || v === undefined) return '—'
  if (typeof v === 'string') return v
  if (col.money) return v.toFixed(2)
  return Number.isInteger(v) ? String(v) : String(Number(v.toFixed(2)))
}
