'use client'

import { useCallback, useState } from 'react'
import { DataTable } from '@/components/data-table/data-table'
import { useStockAdjustmentDataTable } from '../../hooks/stock-adjustment.hooks'
import { getStockAdjustmentHistoryColumns } from '../columns/stock-adjustment-history-columns'
import { StockAdjustmentDetailDialog } from '../dialogs/stock-adjustment-detail-dialog'

export function StockAdjustmentHistoryTable() {
  const [viewId, setViewId] = useState<number | null>(null)
  const getColumns = useCallback(() => getStockAdjustmentHistoryColumns(setViewId), [])

  return (
    <>
      <DataTable
        config={{
          enableRowSelection: false,
          enableSearch: false,
          enableDateFilter: false,
          enableExport: false,
          enableColumnResizing: true,
          enableUrlState: false,
          columnResizingTableId: 'stock-adjustment-history-table',
        }}
        getColumns={getColumns}
        fetchDataFn={useStockAdjustmentDataTable}
        defaultPageSize={20}
        exportConfig={{
          entityName: 'stock-adjustments',
          columnMapping: {},
          columnWidths: [],
          headers: [],
        }}
        idField="id"
      />
      <StockAdjustmentDetailDialog adjustmentId={viewId} onClose={() => setViewId(null)} />
    </>
  )
}
