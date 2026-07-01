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
  useOutletDialogStore,
} from '../../store'
import { useOutletDataTable } from '../../hooks/outlet.hooks'
import { getOutletColumns } from '../columns/outlet-columns'

export function OutletTable() {
  const openCreate = useOutletDialogStore((s) => s.openCreate)
  const { open: openEdit } = useEditDialog()
  const { open: openDelete } = useDeleteDialog()
  const { open: openActivate } = useActivateDialog()
  const { open: openDeactivate } = useDeactivateDialog()

  const getColumns = useCallback(
    () => getOutletColumns({ openEdit, openDelete, openActivate, openDeactivate }),
    [openEdit, openDelete, openActivate, openDeactivate],
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
        columnResizingTableId: 'outlets-table',
        searchPlaceholder: 'Search outlets...',
      }}
      getColumns={getColumns}
      fetchDataFn={useOutletDataTable}
      exportConfig={{
        entityName: 'outlets',
        columnMapping: {
          name: 'Name',
          nicNo: 'NIC No',
          tel: 'Telephone',
          email: 'Email',
          address: 'Address',
          outletType: 'Type',
          outletCategory: 'Category',
          routeName: 'Route',
          isActive: 'Status',
        },
        columnWidths: [
          { wch: 25 },
          { wch: 20 },
          { wch: 15 },
          { wch: 25 },
          { wch: 35 },
          { wch: 12 },
          { wch: 12 },
          { wch: 20 },
          { wch: 10 },
        ],
        headers: [
          'Name',
          'NIC No',
          'Telephone',
          'Email',
          'Address',
          'Type',
          'Category',
          'Route',
          'Status',
        ],
      }}
      idField="id"
      renderToolbarContent={() => (
        <Button onClick={openCreate} className="gap-2">
          <Plus className="h-4 w-4" />
          Add Outlet
        </Button>
      )}
    />
  )
}
