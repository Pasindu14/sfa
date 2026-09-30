'use client'

import { useCallback } from 'react'
import { DataTable } from '@/components/data-table/data-table'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { getRouteUnlockRequestColumns } from '../columns/route-unlock-request-columns'
import {
  usePendingRouteUnlockDataTable,
  useRouteUnlockDataTable,
} from '../../hooks/route-unlock-request.hooks'
import {
  RouteUnlockStatus,
  routeUnlockStatusLabels,
} from '../../schema/route-unlock-request.schema'

const EXPORT_CONFIG = {
  entityName: 'route-unlock-requests',
  columnMapping: {},
  columnWidths: [],
  headers: [],
}

/** Today's pending requests — polled every 60 s by the hook. */
export function PendingRouteUnlockTable() {
  const getColumns = useCallback(() => getRouteUnlockRequestColumns(), [])

  return (
    <DataTable
      config={{
        enableRowSelection: false,
        enableSearch: true,
        enableDateFilter: false,
        enableExport: false,
        enableColumnResizing: true,
        enableUrlState: false,
        columnResizingTableId: 'route-unlock-pending-table',
        searchPlaceholder: 'Search by rep or route...',
      }}
      getColumns={getColumns}
      fetchDataFn={usePendingRouteUnlockDataTable}
      idField="id"
      exportConfig={EXPORT_CONFIG}
    />
  )
}

/** Full history with an effectiveStatus filter and a business-date range. */
export function AllRouteUnlockTable() {
  const getColumns = useCallback(() => getRouteUnlockRequestColumns(), [])

  return (
    <DataTable
      config={{
        enableRowSelection: false,
        enableSearch: true,
        enableDateFilter: true,
        enableExport: false,
        enableColumnResizing: true,
        enableUrlState: false,
        columnResizingTableId: 'route-unlock-all-table',
        searchPlaceholder: 'Search by rep or route...',
      }}
      getColumns={getColumns}
      fetchDataFn={useRouteUnlockDataTable}
      idField="id"
      exportConfig={EXPORT_CONFIG}
      renderCustomFilters={(filters, setFilters) => (
        <Select
          value={(filters?.status as string) || 'all'}
          onValueChange={(value) =>
            setFilters({ ...filters, status: value === 'all' ? '' : value })
          }
        >
          <SelectTrigger className="h-8 w-40">
            <SelectValue placeholder="All Statuses" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">All Statuses</SelectItem>
            {Object.values(RouteUnlockStatus).map((status) => (
              <SelectItem key={status} value={status}>
                {routeUnlockStatusLabels[status]}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
      )}
    />
  )
}
