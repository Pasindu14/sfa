'use client'

import { useCallback } from 'react'
import { DataTable } from '@/components/data-table/data-table'
import { Button } from '@/components/ui/button'
import { Plus } from 'lucide-react'
import {
  useEditDialog,
  useActivateDialog,
  useDeactivateDialog,
  useFleetDialogStore,
} from '../../store'
import { useFleetDataTable } from '../../hooks/fleet.hooks'
import { getFleetColumns } from '../columns/fleet-columns'

export function FleetTable() {
  const openCreate = useFleetDialogStore((s) => s.openCreate)
  const { open: openEdit } = useEditDialog()
  const { open: openActivate } = useActivateDialog()
  const { open: openDeactivate } = useDeactivateDialog()

  const getColumns = useCallback(
    () => getFleetColumns({ openEdit, openActivate, openDeactivate }),
    [openEdit, openActivate, openDeactivate]
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
        columnResizingTableId: 'fleets-table',
        searchPlaceholder: 'Search fleets...',
      }}
      getColumns={getColumns}
      fetchDataFn={useFleetDataTable}
      exportConfig={{
        entityName: 'fleets',
        columnMapping: {
          name: 'Name',
          isActive: 'Status',
        },
        columnWidths: [{ wch: 30 }, { wch: 12 }],
        headers: ['Name', 'Status'],
      }}
      idField="id"
      renderToolbarContent={() => (
        <Button onClick={openCreate} className="gap-2">
          <Plus className="h-4 w-4" />
          Add Fleet
        </Button>
      )}
    />
  )
}
