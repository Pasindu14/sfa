import { create } from 'zustand'
import { devtools } from 'zustand/middleware'
import { toColomboDateStr } from '@/lib/utils/datetime'
import type {
  SalesSummaryFilterIds,
  SalesSummaryGroupBy,
} from '../schema/sales-summary.schema'

// ── Local date helpers ──────────────────────────────────────────────────────
// Dates live here as Colombo `YYYY-MM-DD` strings, never Date objects — building
// them with toISOString() shifts the day for anyone east of UTC.

function fmt(d: Date): string {
  return toColomboDateStr(d)
}

function defaultFrom(): string {
  const now = new Date()
  return fmt(new Date(now.getFullYear(), now.getMonth(), 1))
}

function defaultTo(): string {
  return fmt(new Date())
}

const NO_FILTERS: SalesSummaryFilterIds = {
  regionId: null,
  areaId: null,
  territoryId: null,
  divisionId: null,
  routeId: null,
  distributorId: null,
  salesRepId: null,
  supervisorId: null,
  productId: null,
}

export interface AppliedSalesSummaryFilters extends SalesSummaryFilterIds {
  from: string
  to: string
  groupBy: SalesSummaryGroupBy
  /** Bumped on every Load so pressing it again is a fresh request, not a silent cache hit. */
  loadCount: number
}

interface SalesSummaryFilterState extends SalesSummaryFilterIds {
  from: string
  to: string
  groupBy: SalesSummaryGroupBy
  appliedFilters: AppliedSalesSummaryFilters | null

  setDateRange: (from: string, to: string) => void
  setGroupBy: (groupBy: SalesSummaryGroupBy) => void
  setFilterId: (key: keyof SalesSummaryFilterIds, value: number | null) => void
  clearFilterIds: () => void
  applyFilters: () => void
  reset: () => void
}

/**
 * Live control values are kept separate from `appliedFilters`: an ad-hoc aggregate over an
 * arbitrary date range is far too expensive to re-run on every keystroke, so nothing is fetched
 * until Load copies the live values across.
 */
export const useSalesSummaryFilterStore = create<SalesSummaryFilterState>()(
  devtools(
    (set, get) => ({
      from: defaultFrom(),
      to: defaultTo(),
      groupBy: 'SalesRep',
      ...NO_FILTERS,
      appliedFilters: null,

      setDateRange: (from, to) => set({ from, to }),
      setGroupBy: (groupBy) => set({ groupBy }),
      setFilterId: (key, value) => set({ [key]: value } as Pick<SalesSummaryFilterState, typeof key>),
      clearFilterIds: () => set({ ...NO_FILTERS }),

      applyFilters: () => {
        const s = get()
        if (!s.from || !s.to || s.from > s.to) return
        set({
          appliedFilters: {
            from: s.from,
            to: s.to,
            groupBy: s.groupBy,
            regionId: s.regionId,
            areaId: s.areaId,
            territoryId: s.territoryId,
            divisionId: s.divisionId,
            routeId: s.routeId,
            distributorId: s.distributorId,
            salesRepId: s.salesRepId,
            supervisorId: s.supervisorId,
            productId: s.productId,
            loadCount: (s.appliedFilters?.loadCount ?? 0) + 1,
          },
        })
      },

      reset: () =>
        set({
          from: defaultFrom(),
          to: defaultTo(),
          groupBy: 'SalesRep',
          ...NO_FILTERS,
          appliedFilters: null,
        }),
    }),
    { name: 'SalesSummaryFilterStore' }
  )
)
