'use client'

import { useCallback, useEffect } from 'react'
import { ScrollText } from 'lucide-react'
import { DataTable } from '@/components/data-table/data-table'
import { useStockActivityDataTable } from '../../hooks/stock-activity.hooks'
import { MAX_RANGE_DAYS } from '../../schema/stock-activity.schema'
import { useStockActivityFilters } from '../../store'
import { getStockActivityColumns } from '../columns/stock-activity-columns'
import { StockActivityFilterBar } from '../filters/stock-activity-filter-bar'

export function StockActivityTable() {
  const { appliedFilters, reset } = useStockActivityFilters()

  // Start from a clean slate each visit — the store outlives the page.
  useEffect(() => {
    reset()
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  const getColumns = useCallback(() => getStockActivityColumns(), [])

  return (
    <div className="flex flex-col gap-4">
      <StockActivityFilterBar />

      {appliedFilters ? (
        <DataTable
          key={appliedFilters.runId}
          config={{
            enableRowSelection: false,
            enableSearch: false,
            enableDateFilter: false,
            enableExport: false,
            enableColumnResizing: true,
            enableUrlState: false,
            columnResizingTableId: 'stock-activity-table',
          }}
          getColumns={getColumns}
          fetchDataFn={useStockActivityDataTable}
          defaultPageSize={50}
          exportConfig={{
            entityName: 'stock-activity',
            columnMapping: {},
            columnWidths: [],
            headers: [],
          }}
          idField="id"
        />
      ) : (
        <div className="flex flex-col items-center justify-center gap-2 rounded-lg border border-dashed py-16 text-center">
          <ScrollText className="h-8 w-8 text-muted-foreground/40" />
          <p className="text-sm font-medium text-muted-foreground">
            Choose filters and click <span className="font-semibold">Load Activity</span> to view stock
            movements
          </p>
          <p className="text-xs text-muted-foreground/60">
            Up to {MAX_RANGE_DAYS} days at a time
          </p>
        </div>
      )}
    </div>
  )
}
