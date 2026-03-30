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
  useDivisionDialogStore,
} from '../../store'
import { useDivisionDataTable } from '../../hooks/division.hooks'
import { getDivisionColumns } from '../columns/division-columns'

export function DivisionTable() {
  const openCreate = useDivisionDialogStore((s) => s.openCreate)
  const { open: openEdit } = useEditDialog()
  const { open: openDelete } = useDeleteDialog()
  const { open: openActivate } = useActivateDialog()
  const { open: openDeactivate } = useDeactivateDialog()

  const getColumns = useCallback(
    () => getDivisionColumns({ openEdit, openDelete, openActivate, openDeactivate }),
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
        columnResizingTableId: 'divisions-table',
        searchPlaceholder: 'Search divisions...',
      }}
      getColumns={getColumns}
      fetchDataFn={useDivisionDataTable}
      exportConfig={{
        entityName: 'divisions',
        columnMapping: {
          name: 'Name',
          territoryName: 'Territory',
          areaName: 'Area',
          regionName: 'Region',
          isActive: 'Status',
          createdAt: 'Created At',
        },
        columnWidths: [{ wch: 30 }, { wch: 25 }, { wch: 25 }, { wch: 25 }, { wch: 12 }, { wch: 20 }],
        headers: ['Name', 'Territory', 'Area', 'Region', 'Status', 'Created At'],
      }}
      idField="id"
      renderToolbarContent={() => (
        <Button onClick={openCreate} className="gap-2">
          <Plus className="h-4 w-4" />
          Add Division
        </Button>
      )}
    />
  )
}
