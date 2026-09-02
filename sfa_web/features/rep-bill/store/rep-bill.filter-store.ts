import { create } from 'zustand'
import { devtools } from 'zustand/middleware'
import { toColomboDateStr } from '@/lib/utils/datetime'

function today() {
  return toColomboDateStr(new Date())
}

function startOfThisMonth() {
  const now = new Date()
  return toColomboDateStr(new Date(now.getFullYear(), now.getMonth(), 1))
}

export interface AppliedRepBillFilters {
  dateFrom: string
  dateTo: string
  salesRepId: number
}

interface RepBillFilterState {
  // Live values — updated as the user touches the controls.
  dateFrom: string
  dateTo: string
  supervisorId: number | null
  repId: number | null

  /**
   * Committed values, set only by `applyFilters()`. The data hook reads THIS, never the live
   * values, which is what keeps a query from firing on every keystroke of the date picker.
   *
   * This split also sidesteps a DataTable limitation: its `customFilters` prop is an *initial*
   * value only (it seeds `useConditionalUrlState` once), so filters rendered outside the table
   * cannot reach it by re-rendering. Reading the store directly in the hook bypasses that.
   */
  appliedFilters: AppliedRepBillFilters | null

  /** True between `applyFilters()` and the query settling — drives the button spinner. */
  isFetching: boolean

  setDateRange: (from: string, to: string) => void
  setSupervisorId: (id: number | null) => void
  setRepId: (id: number | null) => void
  applyFilters: () => void
  setFetching: (v: boolean) => void
  reset: () => void
}

const initial = () => ({
  dateFrom: startOfThisMonth(),
  dateTo: today(),
  supervisorId: null,
  repId: null,
  appliedFilters: null,
  isFetching: false,
})

export const useRepBillFilterStore = create<RepBillFilterState>()(
  devtools(
    (set, get) => ({
      ...initial(),

      setDateRange: (dateFrom, dateTo) => set({ dateFrom, dateTo }),

      // Changing supervisor clears the rep. Without this the previously picked rep stays
      // selected while the dropdown now lists someone else's team, and pressing Load would
      // silently fetch the wrong person's bills.
      setSupervisorId: (supervisorId) => set({ supervisorId, repId: null }),

      setRepId: (repId) => set({ repId }),

      applyFilters: () => {
        const { dateFrom, dateTo, repId } = get()
        if (!repId) return
        set({ appliedFilters: { dateFrom, dateTo, salesRepId: repId }, isFetching: true })
      },

      setFetching: (isFetching) => set({ isFetching }),

      reset: () => set(initial()),
    }),
    { name: 'RepBillFilterStore' },
  ),
)
