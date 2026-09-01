'use client'

import { useCallback } from 'react'
import { DataTable } from '@/components/data-table/data-table'
import { Button } from '@/components/ui/button'
import { Plus } from 'lucide-react'
import { useDailyRouteAssignmentDialogStore } from '../../store'
import { useDailyRouteAssignmentDataTable } from '../../hooks/daily-route-assignment.hooks'
import { getDailyRouteAssignmentColumns } from '../columns/daily-route-assignment-columns'

export function DailyRouteAssignmentTable() {
  const openCreate = useDailyRouteAssignmentDialogStore((s) => s.openCreate)
  const openDelete = useDailyRouteAssignmentDialogStore((s) => s.openDelete)

  const getColumns = useCallback(
    (_handleRowDeselection: ((rowId: string) => void) | null | undefined) =>
      getDailyRouteAssignmentColumns({ openDelete }),
    [openDelete],
  )

  return (
    <DataTable
      config={{
        enableRowSelection: false,
        enableSearch: false,
        enableDateFilter: false,
        enableExport: false,
        enableColumnResizing: true,
        enableUrlState: false,
        columnResizingTableId: 'route-assignments-table',
      }}
      getColumns={getColumns}
      fetchDataFn={useDailyRouteAssignmentDataTable as any}
      exportConfig={{
        entityName: 'route-assignments',
        columnMapping: {
          userName: 'Sales Rep',
          routeName: 'Route',
          assignedDate: 'Assigned Date',
          isActive: 'Status',
        },
        columnWidths: [{ wch: 25 }, { wch: 25 }, { wch: 15 }, { wch: 10 }],
        headers: ['Sales Rep', 'Route', 'Assigned Date', 'Status'],
      }}
      idField="id"
      renderToolbarContent={() => (
        <Button onClick={openCreate} className="gap-2">
          <Plus className="h-4 w-4" />
          Assign Route
        </Button>
      )}
    />
  )
}
