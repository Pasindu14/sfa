'use client'

import { useCallback } from 'react'
import { DataTable } from '@/components/data-table/data-table'
import { Button } from '@/components/ui/button'
import {
  useEditDialog,
  useDeleteDialog,
  useChangePasswordDialog,
  useActivateDialog,
  useDeactivateDialog,
  useUserDialogStore,
} from '../../store'
import { getUsersAction } from '../../actions/user.actions'
import { getUserColumns } from '../columns/user-columns'
import type { UserDto } from '../types/user.types'

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

  const fetchData = useCallback(
    async (params: { page: number; limit: number; search?: string }) => {
      const result = await getUsersAction(params.page, params.limit)
      if (!result.success) {
        return {
          success: false,
          data: [] as UserDto[],
          pagination: { page: 1, limit: params.limit, total_pages: 0, total_items: 0 },
        }
      }
      const { users, page, pageSize, totalCount } = result.data

      const term = params.search?.trim().toLowerCase()
      const filtered = term
        ? users.filter(
            (u) =>
              u.name.toLowerCase().includes(term) ||
              u.username.toLowerCase().includes(term) ||
              u.email.toLowerCase().includes(term) ||
              u.phone.toLowerCase().includes(term) ||
              u.role.toLowerCase().includes(term)
          )
        : users

      return {
        success: true,
        data: filtered,
        pagination: {
          page,
          limit: pageSize,
          total_pages: Math.ceil(totalCount / pageSize),
          total_items: totalCount,
        },
      }
    },
    []
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
      fetchDataFn={fetchData as any}
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
      renderToolbarContent={() => (
        <Button onClick={openCreate}>Create User</Button>
      )}
    />
  )
}
