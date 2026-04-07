'use client'

import { useCallback } from 'react'
import { DataTable } from '@/components/data-table/data-table'
import { getRouteCancellationColumns } from '../columns/route-cancellation-columns'
import { useRouteCancellationDataTable } from '../../hooks/route-cancellation.hooks'

export function RouteCancellationTable() {
  const getColumns = useCallback(() => getRouteCancellationColumns(), [])

  return (
    <DataTable
      config={{
        enableRowSelection: false,
        enableSearch: true,
        enableDateFilter: false,
        enableExport: false,
        enableColumnResizing: false,
        enableUrlState: false,
        searchPlaceholder: 'Search by rep name or route...',
      }}
      getColumns={getColumns}
      fetchDataFn={useRouteCancellationDataTable}
      idField="id"
      exportConfig={{
        entityName: 'route-cancellations',
        columnMapping: {},
        columnWidths: [],
        headers: [],
      }}
    />
  )
}
