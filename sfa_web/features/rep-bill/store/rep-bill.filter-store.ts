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
  /**
   * Incremented on every press of Load/Reload.
   *
   * Without it, re-pressing with unchanged filters produces an identical query key, so React
   * Query serves the cached result without ever re-entering a loading state — the spinner would
   * have nothing to switch it back off, and the button would sit on "Loading…" forever.
   *
   * It also makes Reload mean what it says: distributors approve and reject bills out of band,
   * so pressing it must go back to the server rather than re-render stale statuses.
   */
  runId: number
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
        const { dateFrom, dateTo, repId, appliedFilters } = get()
        if (!repId) return
        set({
          appliedFilters: {
            dateFrom,
            dateTo,
            salesRepId: repId,
            runId: (appliedFilters?.runId ?? 0) + 1,
          },
          isFetching: true,
        })
      },

      setFetching: (isFetching) => set({ isFetching }),

      reset: () => set(initial()),
    }),
    { name: 'RepBillFilterStore' },
  ),
)
