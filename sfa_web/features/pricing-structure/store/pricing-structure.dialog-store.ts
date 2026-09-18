import { create } from 'zustand'
import { devtools } from 'zustand/middleware'

/// What the row actions hand the store. rowVersion rides along because set-default is a
/// one-click confirm that needs the concurrency token the row was read with; the name is
/// for confirm copy and the duplicate dialog's "<name> (copy)" default.
export type PricingStructureSelection = {
  id: number
  name: string
  rowVersion: number
}

interface PricingStructureDialogState {
  isCreateOpen: boolean
  isEditOpen: boolean
  isDuplicateOpen: boolean
  isSetDefaultOpen: boolean
  isActivateOpen: boolean
  isDeactivateOpen: boolean
  isDeleteOpen: boolean
  selectedId: number | null
  selectedName: string | null
  selectedRowVersion: number | null
  openCreate: () => void
  closeCreate: () => void
  openEdit: (id: number) => void
  closeEdit: () => void
  openDuplicate: (selection: PricingStructureSelection) => void
  closeDuplicate: () => void
  openSetDefault: (selection: PricingStructureSelection) => void
  closeSetDefault: () => void
  openActivate: (selection: PricingStructureSelection) => void
  closeActivate: () => void
  openDeactivate: (selection: PricingStructureSelection) => void
  closeDeactivate: () => void
  openDelete: (selection: PricingStructureSelection) => void
  closeDelete: () => void
}

const cleared = { selectedId: null, selectedName: null, selectedRowVersion: null }

const select = ({ id, name, rowVersion }: PricingStructureSelection) => ({
  selectedId: id,
  selectedName: name,
  selectedRowVersion: rowVersion,
})

export const usePricingStructureDialogStore = create<PricingStructureDialogState>()(
  devtools(
    (set) => ({
      isCreateOpen: false,
      isEditOpen: false,
      isDuplicateOpen: false,
      isSetDefaultOpen: false,
      isActivateOpen: false,
      isDeactivateOpen: false,
      isDeleteOpen: false,
      ...cleared,
      openCreate: () => set({ isCreateOpen: true }),
      closeCreate: () => set({ isCreateOpen: false }),
      // Edit refetches the structure by id, so the form always starts from a fresh rowVersion.
      openEdit: (id) => set({ isEditOpen: true, selectedId: id }),
      closeEdit: () => set({ isEditOpen: false, ...cleared }),
      openDuplicate: (s) => set({ isDuplicateOpen: true, ...select(s) }),
      closeDuplicate: () => set({ isDuplicateOpen: false, ...cleared }),
      openSetDefault: (s) => set({ isSetDefaultOpen: true, ...select(s) }),
      closeSetDefault: () => set({ isSetDefaultOpen: false, ...cleared }),
      openActivate: (s) => set({ isActivateOpen: true, ...select(s) }),
      closeActivate: () => set({ isActivateOpen: false, ...cleared }),
      openDeactivate: (s) => set({ isDeactivateOpen: true, ...select(s) }),
      closeDeactivate: () => set({ isDeactivateOpen: false, ...cleared }),
      openDelete: (s) => set({ isDeleteOpen: true, ...select(s) }),
      closeDelete: () => set({ isDeleteOpen: false, ...cleared }),
    }),
    { name: 'PricingStructureDialogStore' }
  )
)
