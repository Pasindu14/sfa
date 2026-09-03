import { create } from 'zustand'
import { devtools } from 'zustand/middleware'
import { toColomboDateStr } from '@/lib/utils/datetime'
import {
  GEO_CHAIN,
  ROLE_FILTER_KEY,
  type PeopleRole,
  type SalesSummaryFilterIds,
  type SalesSummaryGroupBy,
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
  asmId: null,
  rsmId: null,
  nsmId: null,
  productId: null,
}

/** Every id a role can write to — cleared whenever the role changes. */
const ALL_ROLE_KEYS = Object.values(ROLE_FILTER_KEY)

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
  /** Which org level the People picker is currently choosing from. */
  role: PeopleRole | null
  appliedFilters: AppliedSalesSummaryFilters | null

  setDateRange: (from: string, to: string) => void
  setGroupBy: (groupBy: SalesSummaryGroupBy) => void
  setGeoId: (key: (typeof GEO_CHAIN)[number], value: number | null) => void
  setFilterId: (key: keyof SalesSummaryFilterIds, value: number | null) => void
  setRole: (role: PeopleRole | null) => void
  setRoleUserId: (value: number | null) => void
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
      role: null,
      ...NO_FILTERS,
      appliedFilters: null,

      setDateRange: (from, to) => set({ from, to }),
      setGroupBy: (groupBy) => set({ groupBy }),

      /**
       * Setting a geo level clears every level BELOW it. A Territory chosen under the old Area is
       * meaningless once the Area changes, and leaving it set would send a contradictory filter pair
       * that returns an empty report with no explanation.
       */
      setGeoId: (key, value) => {
        const idx = GEO_CHAIN.indexOf(key)
        const cleared = Object.fromEntries(
          GEO_CHAIN.slice(idx + 1).map((k) => [k, null])
        ) as Partial<SalesSummaryFilterIds>
        set({ [key]: value, ...cleared } as Partial<SalesSummaryFilterState>)
      },

      setFilterId: (key, value) =>
        set({ [key]: value } as Partial<SalesSummaryFilterState>),

      /** Changing role drops whichever user id the previous role had written. */
      setRole: (role) => {
        const cleared = Object.fromEntries(
          ALL_ROLE_KEYS.map((k) => [k, null])
        ) as Partial<SalesSummaryFilterIds>
        set({ role, ...cleared } as Partial<SalesSummaryFilterState>)
      },

      /** Writes the picked user into the id column matching the current role. */
      setRoleUserId: (value) => {
        const role = get().role
        if (!role) return
        set({ [ROLE_FILTER_KEY[role]]: value } as Partial<SalesSummaryFilterState>)
      },

      clearFilterIds: () => set({ ...NO_FILTERS, role: null }),

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
            asmId: s.asmId,
            rsmId: s.rsmId,
            nsmId: s.nsmId,
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
          role: null,
          ...NO_FILTERS,
          appliedFilters: null,
        }),
    }),
    { name: 'SalesSummaryFilterStore' }
  )
)
