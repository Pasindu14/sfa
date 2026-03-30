'use client'

import { create } from 'zustand'
import { useShallow } from 'zustand/react/shallow'
import { useSalesInvoiceDialogStore } from './sales-invoice.dialog-store'
import { useSalesInvoiceFilterStore } from './sales-invoice.filter-store'

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

// ── Detail dialog ─────────────────────────────────────────────────────────

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

export const useDeleteDialog = () =>
  useSalesInvoiceDialogStore(
    useShallow((s) => ({
      isOpen: s.isDeleteOpen,
      selectedId: s.selectedSalesInvoiceId,
      open: s.openDelete,
      close: s.closeDelete,
    }))
  )

// ── Filter store ───────────────────────────────────────────────────────────

export { useSalesInvoiceFilterStore }

export const useSalesInvoiceFilters = () =>
  useSalesInvoiceFilterStore(
    useShallow((s) => ({
      date: s.date,
      distributorId: s.distributorId,
      appliedFilters: s.appliedFilters,
      setDate: s.setDate,
      setDistributorId: s.setDistributorId,
      applyFilters: s.applyFilters,
      reset: s.reset,
    }))
  )
