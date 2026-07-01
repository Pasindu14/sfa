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
  useRouteDialogStore,
} from '../../store'
import { useRouteDataTable } from '../../hooks/route.hooks'
import { getRouteColumns } from '../columns/route-columns'

export function RouteTable() {
  const openCreate = useRouteDialogStore((s) => s.openCreate)
  const { open: openEdit } = useEditDialog()
  const { open: openDelete } = useDeleteDialog()
  const { open: openActivate } = useActivateDialog()
  const { open: openDeactivate } = useDeactivateDialog()

  const getColumns = useCallback(
    () => getRouteColumns({ openEdit, openDelete, openActivate, openDeactivate }),
    [openEdit, openDelete, openActivate, openDeactivate],
  );

  return (
    <DataTable
      config={{
        enableRowSelection: false,
        enableSearch: true,
        enableDateFilter: false,
        enableExport: false,
        enableColumnResizing: true,
        enableUrlState: false,
        columnResizingTableId: 'routes-table',
        searchPlaceholder: 'Search routes...',
      }}
      getColumns={getColumns}
      fetchDataFn={useRouteDataTable}
      exportConfig={{
        entityName: 'routes',
        columnMapping: {
          name: 'Name',
          pinColor: 'Pin Color',
          divisionName: 'Division',
          territoryName: 'Territory',
          areaName: 'Area',
          regionName: 'Region',
          createdAt: 'Created At',
        },
        columnWidths: [
          { wch: 30 },
          { wch: 12 },
          { wch: 25 },
          { wch: 25 },
          { wch: 25 },
          { wch: 25 },
          { wch: 20 },
        ],
        headers: ['Name', 'Pin Color', 'Division', 'Territory', 'Area', 'Region', 'Created At'],
      }}
      idField="id"
      renderToolbarContent={() => (
        <Button onClick={openCreate} className="gap-2">
          <Plus className="h-4 w-4" />
          Add Route
        </Button>
      )}
    />
  )
}
