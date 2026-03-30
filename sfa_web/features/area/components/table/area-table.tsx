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
  useAreaDialogStore,
} from '../../store'
import { useAreaDataTable } from '../../hooks/area.hooks'
import { getAreaColumns } from '../columns/area-columns'

export function AreaTable() {
  const openCreate = useAreaDialogStore((s) => s.openCreate)
  const { open: openEdit } = useEditDialog()
  const { open: openDelete } = useDeleteDialog()
  const { open: openActivate } = useActivateDialog()
  const { open: openDeactivate } = useDeactivateDialog()

  const getColumns = useCallback(
    () => getAreaColumns({ openEdit, openDelete, openActivate, openDeactivate }),
    [openEdit, openDelete, openActivate, openDeactivate],
  )

  return (
    <DataTable
      config={{
        enableRowSelection: false,
        enableSearch: true,
        enableDateFilter: false,
        enableExport: false,
        enableColumnResizing: false,
        enableUrlState: false,
        columnResizingTableId: 'areas-table',
        searchPlaceholder: 'Search areas...',
      }}
      getColumns={getColumns}
      fetchDataFn={useAreaDataTable as any}
      exportConfig={{
        entityName: 'areas',
        columnMapping: {
          name: 'Name',
          regionName: 'Region',
          isActive: 'Status',
          createdAt: 'Created At',
        },
        columnWidths: [{ wch: 30 }, { wch: 25 }, { wch: 12 }, { wch: 20 }],
        headers: ['Name', 'Region', 'Status', 'Created At'],
      }}
      idField="id"
      renderToolbarContent={() => (
        <Button onClick={openCreate} className="gap-2">
          <Plus className="h-4 w-4" />
          Add Area
        </Button>
      )}
    />
  )
}
