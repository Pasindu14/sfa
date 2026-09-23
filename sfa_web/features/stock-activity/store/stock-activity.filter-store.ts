import { create } from 'zustand'
import { devtools } from 'zustand/middleware'
import { toColomboDateStr } from '@/lib/utils/datetime'
import type {
  StockActivityFilters,
  StockDirection,
  StockTransactionType,
} from '../schema/stock-activity.schema'

function today() {
  return toColomboDateStr(new Date())
}

function sixDaysAgo() {
  return toColomboDateStr(new Date(Date.now() - 6 * 24 * 60 * 60 * 1000))
}

export interface AppliedStockActivityFilters extends StockActivityFilters {
  /**
   * Incremented on every Load/Reload so re-pressing with unchanged filters still produces a new
   * query key and goes back to the server — stock moves constantly.
   */
  runId: number
}

interface StockActivityFilterState {
  // Live values — updated as the user touches the controls.
  from: string
  to: string
  distributorId: number | null
  productId: number | null
  userId: number | null
  transactionType: StockTransactionType | null
  direction: StockDirection | null

  /** Committed values, set only by `applyFilters()`. The data hook reads THIS, never the live values. */
  appliedFilters: AppliedStockActivityFilters | null

  setDateRange: (from: string, to: string) => void
  setDistributorId: (id: number | null) => void
  setProductId: (id: number | null) => void
  setUserId: (id: number | null) => void
  setTransactionType: (type: StockTransactionType | null) => void
  setDirection: (direction: StockDirection | null) => void
  applyFilters: () => void
  reset: () => void
}

// Default window: the last 7 days, today inclusive.
const initial = () => ({
  from: sixDaysAgo(),
  to: today(),
  distributorId: null,
  productId: null,
  userId: null,
  transactionType: null,
  direction: null,
  appliedFilters: null,
})

export const useStockActivityFilterStore = create<StockActivityFilterState>()(
  devtools(
    (set, get) => ({
      ...initial(),
      setDateRange: (from, to) => set({ from, to }),
      setDistributorId: (distributorId) => set({ distributorId }),
      setProductId: (productId) => set({ productId }),
      setUserId: (userId) => set({ userId }),
      setTransactionType: (transactionType) => set({ transactionType }),
      setDirection: (direction) => set({ direction }),
      applyFilters: () => {
        const s = get()
        if (!s.from || !s.to) return
        set({
          appliedFilters: {
            from: s.from,
            to: s.to,
            distributorId: s.distributorId,
            productId: s.productId,
            userId: s.userId,
            transactionType: s.transactionType,
            direction: s.direction,
            runId: (s.appliedFilters?.runId ?? 0) + 1,
          },
        })
      },
      reset: () => set(initial()),
    }),
    { name: 'StockActivityFilterStore' }
  )
)
