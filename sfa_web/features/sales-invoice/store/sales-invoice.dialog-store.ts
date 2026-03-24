import { create } from 'zustand'
import { devtools } from 'zustand/middleware'

interface SalesInvoiceDialogState {
  isDetailOpen: boolean
  selectedSalesInvoiceId: number | null
  openDetail: (id: number) => void
  closeDetail: () => void
}

export const useSalesInvoiceDialogStore = create<SalesInvoiceDialogState>()(
  devtools(
    (set) => ({
      isDetailOpen: false,
      selectedSalesInvoiceId: null,
      openDetail: (id) => set({ isDetailOpen: true, selectedSalesInvoiceId: id }),
      closeDetail: () => set({ isDetailOpen: false, selectedSalesInvoiceId: null }),
    }),
    { name: 'SalesInvoiceDialogStore' }
  )
)
