'use client'

import { useCallback } from 'react'
import { DataTable } from '@/components/data-table/data-table'
import { Button } from '@/components/ui/button'
import { Plus } from 'lucide-react'
import { useUserGeoAssignmentDialogStore } from '../../store'
import { useUserGeoAssignmentDataTable } from '../../hooks/user-geo-assignment.hooks'
import { getUserGeoAssignmentColumns } from '../columns/user-geo-assignment-columns'

export function UserGeoAssignmentTable() {
  const openCreate = useUserGeoAssignmentDialogStore((s) => s.openCreate)
  const openEdit = useUserGeoAssignmentDialogStore((s) => s.openEdit)
  const openDeactivate = useUserGeoAssignmentDialogStore((s) => s.openDeactivate)
  const openActivate = useUserGeoAssignmentDialogStore((s) => s.openActivate)

  const getColumns = useCallback(
    (_handleRowDeselection: ((rowId: string) => void) | null | undefined) =>
      getUserGeoAssignmentColumns({ openEdit, openDeactivate, openActivate }),
    [openEdit, openDeactivate, openActivate],
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
        columnResizingTableId: 'user-geo-assignments-table',
        searchPlaceholder: 'Search by name…',
      }}
      getColumns={getColumns}
      fetchDataFn={useUserGeoAssignmentDataTable as any}
      exportConfig={{
        entityName: 'user-geo-assignments',
        columnMapping: {
          userName: 'Name',
          userRole: 'Role',
          divisionName: 'Division',
          regionName: 'Region',
          reportsToUserName: 'Reports To',
          effectiveFrom: 'Assigned On',
          isActive: 'Status',
        },
        columnWidths: [
          { wch: 25 }, { wch: 15 }, { wch: 25 }, { wch: 20 }, { wch: 25 }, { wch: 15 }, { wch: 10 },
        ],
        headers: ['Name', 'Role', 'Division', 'Region', 'Reports To', 'Assigned On', 'Status'],
      }}
      idField="id"
      renderToolbarContent={() => (
        <Button onClick={openCreate} className="gap-2 bg-orange-500 hover:bg-orange-600 text-white">
          <Plus className="h-4 w-4" />
          Add Assignment
        </Button>
      )}
    />
  )
}
