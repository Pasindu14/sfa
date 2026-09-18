'use client'

import { useCallback } from 'react'
import { DataTable } from '@/components/data-table/data-table'
import { Button } from '@/components/ui/button'
import { Plus } from 'lucide-react'
import {
  useEditDialog,
  useDuplicateDialog,
  useSetDefaultDialog,
  useActivateDialog,
  useDeactivateDialog,
  useDeleteDialog,
  usePricingStructureDialogStore,
} from '../../store'
import { usePricingStructureDataTable } from '../../hooks/pricing-structure.hooks'
import { getPricingStructureColumns } from '../columns/pricing-structure-columns'

export function PricingStructureTable() {
  const openCreate = usePricingStructureDialogStore((s) => s.openCreate)
  const { open: openEdit } = useEditDialog()
  const { open: openDuplicate } = useDuplicateDialog()
  const { open: openSetDefault } = useSetDefaultDialog()
  const { open: openActivate } = useActivateDialog()
  const { open: openDeactivate } = useDeactivateDialog()
  const { open: openDelete } = useDeleteDialog()

  const getColumns = useCallback(
    () =>
      getPricingStructureColumns({
        openEdit,
        openDuplicate,
        openSetDefault,
        openActivate,
        openDeactivate,
        openDelete,
      }),
    [openEdit, openDuplicate, openSetDefault, openActivate, openDeactivate, openDelete]
  )

  return (
    <DataTable
      config={{
        enableRowSelection: false,
        enableSearch: true,
        enableDateFilter: false,
        enableExport: false,
        enableColumnResizing: true,
        enableUrlState: false,
        columnResizingTableId: 'pricing-structures-table',
        searchPlaceholder: 'Search by name...',
      }}
      getColumns={getColumns}
      fetchDataFn={usePricingStructureDataTable}
      exportConfig={{
        entityName: 'pricing-structures',
        columnMapping: {
          name: 'Name',
          description: 'Description',
          pricedCount: 'Priced Products',
          isActive: 'Status',
        },
        columnWidths: [{ wch: 30 }, { wch: 40 }, { wch: 15 }, { wch: 12 }],
        headers: ['Name', 'Description', 'Priced Products', 'Status'],
      }}
      idField="id"
      renderToolbarContent={() => (
        <Button onClick={openCreate} className="gap-2">
          <Plus className="h-4 w-4" />
          Add Pricing Structure
        </Button>
      )}
    />
  )
}
