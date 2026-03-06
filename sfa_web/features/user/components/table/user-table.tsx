'use client'

import { useCallback } from 'react'
import { DataTable } from '@/components/data-table/data-table'
import { Button } from '@/components/ui/button'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { UserPlus } from 'lucide-react'
import {
  useEditDialog,
  useDeleteDialog,
  useChangePasswordDialog,
  useActivateDialog,
  useDeactivateDialog,
  useUserDialogStore,
} from '../../store'
import { useUserDataTable } from '../../hooks/user.hooks'
import { getUserColumns } from '../columns/user-columns'

export function UserTable() {
  const openCreate = useUserDialogStore((s) => s.openCreate)
  const { open: openEdit } = useEditDialog()
  const { open: openDelete } = useDeleteDialog()
  const { open: openChangePassword } = useChangePasswordDialog()
  const { open: openActivate } = useActivateDialog()
  const { open: openDeactivate } = useDeactivateDialog()

  const getColumns = useCallback(
    (_handleRowDeselection: ((rowId: string) => void) | null | undefined) =>
      getUserColumns({ openEdit, openDelete, openChangePassword, openActivate, openDeactivate }),
    [openEdit, openDelete, openChangePassword, openActivate, openDeactivate]
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
        columnResizingTableId: 'users-table',
        searchPlaceholder: 'Search users...',
      }}
      getColumns={getColumns}
      fetchDataFn={useUserDataTable as any}
      exportConfig={{
        entityName: 'users',
        columnMapping: {
          name: 'Name',
          username: 'Username',
          email: 'Email',
          phone: 'Phone',
          role: 'Role',
          isActive: 'Status',
          createdAt: 'Created At',
        },
        columnWidths: [
          { wch: 25 },
          { wch: 15 },
          { wch: 25 },
          { wch: 15 },
          { wch: 12 },
          { wch: 10 },
          { wch: 20 },
        ],
        headers: ['Name', 'Username', 'Email', 'Phone', 'Role', 'Status', 'Created At'],
      }}
      idField="id"
      renderCustomFilters={(filters, setFilters) => (
        <Select
          value={filters?.role ?? ''}
          onValueChange={(value) => setFilters({ ...filters, role: value === 'all' ? '' : value })}
        >
          <SelectTrigger className="h-8 w-[130px]">
            <SelectValue placeholder="All Roles" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">All Roles</SelectItem>
            <SelectItem value="Admin">Admin</SelectItem>
            <SelectItem value="Manager">Manager</SelectItem>
            <SelectItem value="SalesRep">Sales Rep</SelectItem>
          </SelectContent>
        </Select>
      )}
      renderToolbarContent={() => (
        <Button onClick={openCreate} className="gap-2">
          <UserPlus className="h-4 w-4" />
          Add User
        </Button>
      )}
    />
  )
}
