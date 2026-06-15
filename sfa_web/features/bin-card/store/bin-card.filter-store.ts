import { create } from 'zustand'
import { devtools } from 'zustand/middleware'

// ── Local date helpers (avoid UTC drift from toISOString) ───────────────────

function fmt(d: Date): string {
  const y = d.getFullYear()
  const m = String(d.getMonth() + 1).padStart(2, '0')
  const day = String(d.getDate()).padStart(2, '0')
  return `${y}-${m}-${day}`
}

function defaultFrom(): string {
  const now = new Date()
  return fmt(new Date(now.getFullYear(), now.getMonth(), 1))
}

function defaultTo(): string {
  return fmt(new Date())
}

export interface AppliedBinCardFilters {
  distributorId: number
  from: string
  to: string
  loadCount: number
}

interface BinCardFilterState {
  distributorId: number | null
  from: string
  to: string
  appliedFilters: AppliedBinCardFilters | null
  setDistributorId: (id: number | null) => void
  setFrom: (d: string) => void
  setTo: (d: string) => void
  applyFilters: () => void
  reset: () => void
}

export const useBinCardFilterStore = create<BinCardFilterState>()(
  devtools(
    (set, get) => ({
      distributorId: null,
      from: defaultFrom(),
      to: defaultTo(),
      appliedFilters: null,
      setDistributorId: (distributorId) => set({ distributorId }),
      setFrom: (from) => set({ from }),
      setTo: (to) => set({ to }),
      applyFilters: () => {
        const { distributorId, from, to, appliedFilters } = get()
        if (!distributorId || !from || !to) return
        set({
          appliedFilters: {
            distributorId,
            from,
            to,
            loadCount: (appliedFilters?.loadCount ?? 0) + 1,
          },
        })
      },
      reset: () =>
        set({
          distributorId: null,
          from: defaultFrom(),
          to: defaultTo(),
          appliedFilters: null,
        }),
    }),
    { name: 'BinCardFilterStore' }
  )
)
