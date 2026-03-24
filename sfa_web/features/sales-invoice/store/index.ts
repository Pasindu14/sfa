'use client'

import { create } from 'zustand'
import { useShallow } from 'zustand/react/shallow'
import { useSalesInvoiceDialogStore } from './sales-invoice.dialog-store'

// ── Import dialog (kept as-is — used by SalesInvoiceImportDialog) ─────────

interface ImportDialogStore {
  isOpen: boolean
  open: () => void
  close: () => void
}

const useImportDialogStore = create<ImportDialogStore>((set) => ({
  isOpen: false,
  open: () => set({ isOpen: true }),
  close: () => set({ isOpen: false }),
}))

export function useImportDialog() {
  return useImportDialogStore(
    useShallow((s) => ({ isOpen: s.isOpen, open: s.open, close: s.close }))
  )
}

// ── Detail drawer ─────────────────────────────────────────────────────────

export { useSalesInvoiceDialogStore }

export const useDetailDialog = () =>
  useSalesInvoiceDialogStore(
    useShallow((s) => ({
      isOpen: s.isDetailOpen,
      selectedId: s.selectedSalesInvoiceId,
      open: s.openDetail,
      close: s.closeDetail,
    }))
  )
