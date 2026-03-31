import { create } from 'zustand'
import { devtools } from 'zustand/middleware'

interface SalesInvoiceDialogState {
  isDetailOpen: boolean
  isDeleteOpen: boolean
  isCreateGrnOpen: boolean
  selectedSalesInvoiceId: number | null
  openDetail: (id: number) => void
  closeDetail: () => void
  openDelete: (id: number) => void
  closeDelete: () => void
  openCreateGrn: (id: number) => void
  closeCreateGrn: () => void
}

export const useSalesInvoiceDialogStore = create<SalesInvoiceDialogState>()(
  devtools(
    (set) => ({
      isDetailOpen: false,
      isDeleteOpen: false,
      isCreateGrnOpen: false,
      selectedSalesInvoiceId: null,
      openDetail: (id) => set({ isDetailOpen: true, selectedSalesInvoiceId: id }),
      closeDetail: () => set({ isDetailOpen: false, selectedSalesInvoiceId: null }),
      openDelete: (id) => set({ isDeleteOpen: true, selectedSalesInvoiceId: id }),
      closeDelete: () => set({ isDeleteOpen: false, selectedSalesInvoiceId: null }),
      openCreateGrn: (id) => set({ isCreateGrnOpen: true, selectedSalesInvoiceId: id }),
      closeCreateGrn: () => set({ isCreateGrnOpen: false, selectedSalesInvoiceId: null }),
    }),
    { name: 'SalesInvoiceDialogStore' }
  )
)
