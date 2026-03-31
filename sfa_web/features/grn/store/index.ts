'use client'

import { create } from 'zustand'
import { useShallow } from 'zustand/react/shallow'
import { useGrnFilterStore } from './grn.filter-store'

export { useGrnFilterStore }

// ── Confirm dialog store ───────────────────────────────────────────────────

interface ConfirmDialogStore {
  isOpen: boolean
  selectedGrnId: number | null
  open: (id: number) => void
  close: () => void
}

const useConfirmDialogStore = create<ConfirmDialogStore>((set) => ({
  isOpen: false,
  selectedGrnId: null,
  open: (id) => set({ isOpen: true, selectedGrnId: id }),
  close: () => set({ isOpen: false, selectedGrnId: null }),
}))

export function useConfirmDialog() {
  return useConfirmDialogStore(
    useShallow((s) => ({
      isOpen: s.isOpen,
      selectedGrnId: s.selectedGrnId,
      open: s.open,
      close: s.close,
    }))
  )
}

// ── Delete dialog store ────────────────────────────────────────────────────

interface DeleteDialogStore {
  isOpen: boolean
  selectedGrnId: number | null
  open: (id: number) => void
  close: () => void
}

const useDeleteDialogStore = create<DeleteDialogStore>((set) => ({
  isOpen: false,
  selectedGrnId: null,
  open: (id) => set({ isOpen: true, selectedGrnId: id }),
  close: () => set({ isOpen: false, selectedGrnId: null }),
}))

export function useDeleteDialog() {
  return useDeleteDialogStore(
    useShallow((s) => ({
      isOpen: s.isOpen,
      selectedGrnId: s.selectedGrnId,
      open: s.open,
      close: s.close,
    }))
  )
}

// ── Filter store selectors ─────────────────────────────────────────────────

export const useGrnFilters = () =>
  useGrnFilterStore(
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
