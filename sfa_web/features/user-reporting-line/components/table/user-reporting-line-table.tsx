'use client'

import { useCallback } from 'react'
import { DataTable } from '@/components/data-table/data-table'
import { Button } from '@/components/ui/button'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { AsyncSelect } from '@/components/async-select'
import { Plus } from 'lucide-react'
import { useUserReportingLineDialogStore } from '../../store'
import { useUserReportingLineDataTable, useUsersForSelect } from '../../hooks/user-reporting-line.hooks'
import { getUserReportingLineColumns } from '../columns/user-reporting-line-columns'
import type { UserDto } from '@/features/user/schema/user.schema'

const ROLES = ['NSM', 'RSM', 'ASM', 'Supervisor', 'SalesRep']

export function UserReportingLineTable() {
  const openCreate = useUserReportingLineDialogStore((s) => s.openCreate)
  const openEdit = useUserReportingLineDialogStore((s) => s.openEdit)
  const openDeactivate = useUserReportingLineDialogStore((s) => s.openDeactivate)
  const openActivate = useUserReportingLineDialogStore((s) => s.openActivate)

  // Same cache used by the form dialogs — zero extra network calls
  const { data: users = [] } = useUsersForSelect()
  const managerPool = users.filter((u) => u.role !== 'Admin' && u.isActive)

  const managerFilterFetcher = useCallback(
    async (query?: string): Promise<UserDto[]> => {
      if (!query) return managerPool
      return managerPool.filter((u) =>
        u.name.toLowerCase().includes(query.toLowerCase()),
      )
    },
    [managerPool],
  )

  const getColumns = useCallback(
    (_handleRowDeselection: ((rowId: string) => void) | null | undefined) =>
      getUserReportingLineColumns({ openEdit, openDeactivate, openActivate }),
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
        columnResizingTableId: 'user-reporting-lines-table',
        searchPlaceholder: 'Search by name…',
      }}
      getColumns={getColumns}
      fetchDataFn={useUserReportingLineDataTable as any}
      exportConfig={{
        entityName: 'user-reporting-lines',
        columnMapping: {
          userName: 'Subordinate',
          userRole: 'Role',
          reportsToUserName: 'Reports To',
          reportsToUserRole: 'Manager Role',
          effectiveFrom: 'Effective From',
          isActive: 'Status',
        },
        columnWidths: [{ wch: 25 }, { wch: 15 }, { wch: 25 }, { wch: 15 }, { wch: 15 }, { wch: 10 }],
        headers: ['Subordinate', 'Role', 'Reports To', 'Manager Role', 'Effective From', 'Status'],
      }}
      idField="id"
      renderCustomFilters={(filters, setFilters) => (
        <div className="flex items-center gap-2">
          {/* Subordinate role filter */}
          <Select
            value={(filters?.role as string) ?? ''}
            onValueChange={(v) =>
              setFilters({ ...filters, role: v === 'all' ? '' : v })
            }
          >
            <SelectTrigger className="h-8 w-36">
              <SelectValue placeholder="All Roles" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">All Roles</SelectItem>
              {ROLES.map((r) => (
                <SelectItem key={r} value={r}>
                  {r}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>

          {/* Manager filter — AsyncSelect, all users except Admin */}
          <AsyncSelect<UserDto>
            fetcher={managerFilterFetcher}
            preload={false}
            label="manager"
            placeholder="All Managers"
            value={filters?.reportsToUserId ? String(filters.reportsToUserId as string) : ''}
            onChange={(v) =>
              setFilters({
                ...filters,
                reportsToUserId: v ? Number(v) : undefined,
              })
            }
            getOptionValue={(u) => String(u.id)}
            getDisplayValue={(u) => <span className="text-sm">{u.name}</span>}
            renderOption={(u) => (
              <div className="flex flex-col">
                <span className="text-sm">{u.name}</span>
                <span className="text-xs text-muted-foreground">{u.role}</span>
              </div>
            )}
            noResultsMessage="No users found"
            clearable
            width="180px"
            triggerClassName="h-8 text-sm"
          />
        </div>
      )}
      renderToolbarContent={() => (
        <Button onClick={openCreate} className="gap-2">
          <Plus className="h-4 w-4" />
          Add Reporting Line
        </Button>
      )}
    />
  )
}
