import type {
  SalesSummaryGroupBy,
  SalesSummaryRow,
  SalesSummaryTotals,
} from '../../schema/sales-summary.schema'

export type CellValue = string | number | null

/**
 * Column bands.
 *
 * Fifteen adjacent numeric columns are unreadable as a flat row — the eye has nothing to hold on
 * to. Grouping them into five bands with a second header tier, an alternating tint and a rule
 * between bands turns "which column is this" into a glance. The grouping is real information about
 * the figures, not decoration: each band is a distinct stage of the calculation.
 */
export type BandId = 'label' | 'target' | 'sales' | 'returns' | 'discounts' | 'net'

export interface Band {
  id: BandId
  /** Sentence case on purpose — a row of caps reads as shouting above a quiet table. */
  header: string
  /** Alternate bands carry a faint tint so the eye can find a band without counting columns. */
  tinted: boolean
}

export const BANDS: Band[] = [
  { id: 'label',     header: '',          tinted: false },
  { id: 'target',    header: 'Target',    tinted: true },
  { id: 'sales',     header: 'Sales',     tinted: false },
  { id: 'returns',   header: 'Returns',   tinted: true },
  { id: 'discounts', header: 'Discounts', tinted: false },
  { id: 'net',       header: 'Net',       tinted: true },
]

export interface SalesSummaryColumn {
  key: string
  header: string
  band: BandId
  /** Cell value for a data row. */
  get: (row: SalesSummaryRow) => CellValue
  /** Cell value for the totals row (omitted = blank in totals). */
  getTotal?: (t: SalesSummaryTotals) => CellValue
  align: 'left' | 'right'
  /** Render with 2 decimal places (money and percentages). */
  money?: boolean
  /** Rendered as an inline achievement meter rather than a figure. */
  meter?: boolean
}

/** The label column's header follows the grouping the user picked. */
const GROUP_HEADER: Record<SalesSummaryGroupBy, string> = {
  SalesRep: 'Sales rep',
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
 * There is deliberately no "Date range" column: the range is a filter, and it appears once in the
 * report header and once in the export title.
 *
 * Two intentional asymmetries carried over from the specification — Gross sale value is already net
 * of good returns, and Net sale qty deducts good return qty only (damage/expiry stock never
 * re-enters saleable inventory, so its quantity is never subtracted).
 */
export function buildSalesSummaryColumns(groupBy: SalesSummaryGroupBy): SalesSummaryColumn[] {
  const isProduct = groupBy === 'Product'

  return [
    ...(isProduct
      ? [
          {
            key: 'groupCode',
            header: 'Code',
            band: 'label' as const,
            get: (r: SalesSummaryRow) => r.groupCode,
            align: 'left' as const,
          },
        ]
      : []),
    {
      key: 'groupName',
      header: GROUP_HEADER[groupBy],
      band: 'label',
      get: (r) => r.groupName,
      align: 'left',
    },

    { key: 'targetValue',     header: 'Value',   band: 'target',    get: (r) => r.targetValue,     getTotal: (t) => t.targetValue,     align: 'right', money: true },
    { key: 'targetQty',       header: 'Qty',     band: 'target',    get: (r) => r.targetQty,       getTotal: (t) => t.targetQty,       align: 'right' },

    { key: 'grossSaleValue',  header: 'Gross',   band: 'sales',     get: (r) => r.grossSaleValue,  getTotal: (t) => t.grossSaleValue,  align: 'right', money: true },
    { key: 'saleQty',         header: 'Qty',     band: 'sales',     get: (r) => r.saleQty,         getTotal: (t) => t.saleQty,         align: 'right' },

    { key: 'goodReturn',      header: 'Good',    band: 'returns',   get: (r) => r.goodReturn,      getTotal: (t) => t.goodReturn,      align: 'right', money: true },
    { key: 'goodReturnQty',   header: 'Good qty', band: 'returns',  get: (r) => r.goodReturnQty,   getTotal: (t) => t.goodReturnQty,   align: 'right' },
    { key: 'marketReturn',    header: 'Market',  band: 'returns',   get: (r) => r.marketReturn,    getTotal: (t) => t.marketReturn,    align: 'right', money: true },
    { key: 'marketReturnQty', header: 'Market qty', band: 'returns', get: (r) => r.marketReturnQty, getTotal: (t) => t.marketReturnQty, align: 'right' },

    { key: 'dbDiscount',      header: 'DB',      band: 'discounts', get: (r) => r.dbDiscount,      getTotal: (t) => t.dbDiscount,      align: 'right', money: true },
    { key: 'discount',        header: 'Item',    band: 'discounts', get: (r) => r.discount,        getTotal: (t) => t.discount,        align: 'right', money: true },

    { key: 'netSaleValue',    header: 'Value',   band: 'net',       get: (r) => r.netSaleValue,    getTotal: (t) => t.netSaleValue,    align: 'right', money: true },
    { key: 'netSaleQty',      header: 'Qty',     band: 'net',       get: (r) => r.netSaleQty,      getTotal: (t) => t.netSaleQty,      align: 'right' },
    { key: 'achievementPercent', header: 'Achieved', band: 'net',   get: (r) => r.achievementPercent, getTotal: (t) => t.achievementPercent, align: 'right', money: true, meter: true },
  ]
}

/** Format a cell for display. An em dash means "not measurable", which is not the same as zero. */
export function formatCell(v: CellValue, col: SalesSummaryColumn): string {
  if (v === null || v === undefined) return '—'
  if (typeof v === 'string') return v
  if (col.money) return v.toFixed(2)
  return Number.isInteger(v) ? String(v) : String(Number(v.toFixed(2)))
}

/** Thousands separators for on-screen figures; the exports keep raw numbers. */
export function formatDisplay(v: CellValue, col: SalesSummaryColumn): string {
  if (v === null || v === undefined) return '—'
  if (typeof v === 'string') return v
  return v.toLocaleString('en-LK', {
    minimumFractionDigits: col.money ? 2 : 0,
    maximumFractionDigits: col.money ? 2 : 2,
  })
}
