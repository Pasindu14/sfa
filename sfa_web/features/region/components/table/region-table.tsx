'use client'

import { useCallback } from 'react'
import { DataTable } from '@/components/data-table/data-table'
import { Button } from '@/components/ui/button'
import { Plus } from 'lucide-react'
import {
  useEditDialog,
  useDeleteDialog,
  useActivateDialog,
  useDeactivateDialog,
  useRegionDialogStore,
} from '../../store'
import { useRegionDataTable } from '../../hooks/region.hooks'
import { getRegionColumns } from '../columns/region-columns'

export function RegionTable() {
  const openCreate = useRegionDialogStore((s) => s.openCreate)
  const { open: openEdit } = useEditDialog()
  const { open: openDelete } = useDeleteDialog()
  const { open: openActivate } = useActivateDialog()
  const { open: openDeactivate } = useDeactivateDialog()

  const getColumns = useCallback(
    () => getRegionColumns({ openEdit, openDelete, openActivate, openDeactivate }),
    [openEdit, openDelete, openActivate, openDeactivate]
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
        columnResizingTableId: 'regions-table',
        searchPlaceholder: 'Search by name or code...',
      }}
      getColumns={getColumns}
      fetchDataFn={useRegionDataTable as any}
      exportConfig={{
        entityName: 'regions',
        columnMapping: {
          name: 'Name',
          isActive: 'Status',
          createdAt: 'Created At',
        },
        columnWidths: [{ wch: 30 }, { wch: 12 }, { wch: 20 }],
        headers: ['Name', 'Status', 'Created At'],
      }}
      idField="id"
      renderToolbarContent={() => (
        <Button onClick={openCreate} className="gap-2">
          <Plus className="h-4 w-4" />
          Add Region
        </Button>
      )}
    />
  )
}
