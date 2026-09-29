import { create } from 'zustand'
import { devtools } from 'zustand/middleware'

interface DashboardState {
  /** Colombo `YYYY-MM-DD`; null means "today", which keeps auto-refreshing across midnight. */
  date: string | null
  setDate: (date: string | null) => void
}

export const useDashboardStore = create<DashboardState>()(
  devtools(
    (set) => ({
      date: null,
      setDate: (date) => set({ date }, false, 'dashboard/setDate'),
    }),
    { name: 'dashboard-store' },
  ),
)
