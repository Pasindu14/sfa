'use client'

import { useShallow } from 'zustand/react/shallow'
import { useRepBillDialogStore } from './rep-bill.dialog-store'
import { useRepBillFilterStore } from './rep-bill.filter-store'

export { useRepBillDialogStore, useRepBillFilterStore }

export const useRepBillDetailDialog = () =>
  useRepBillDialogStore(
    useShallow((s) => ({
      isOpen: s.isDetailOpen,
      selectedId: s.selectedBillId,
      open: s.openDetail,
      close: s.closeDetail,
    })),
  )

export const useRepBillFilters = () =>
  useRepBillFilterStore(
    useShallow((s) => ({
      dateFrom: s.dateFrom,
      dateTo: s.dateTo,
      supervisorId: s.supervisorId,
      repId: s.repId,
      appliedFilters: s.appliedFilters,
      isFetching: s.isFetching,
      setDateRange: s.setDateRange,
      setSupervisorId: s.setSupervisorId,
      setRepId: s.setRepId,
      applyFilters: s.applyFilters,
      reset: s.reset,
    })),
  )
