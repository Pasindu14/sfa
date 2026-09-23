'use client'

import { useCallback, useState } from 'react'
import { DataTable } from '@/components/data-table/data-table'
import { useStockTransferDataTable } from '../../hooks/stock-transfer.hooks'
import { getStockTransferHistoryColumns } from '../columns/stock-transfer-history-columns'
import { StockTransferDetailDialog } from '../dialogs/stock-transfer-detail-dialog'

export function StockTransferHistoryTable() {
  const [viewId, setViewId] = useState<number | null>(null)
  const getColumns = useCallback(() => getStockTransferHistoryColumns(setViewId), [])

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
          columnResizingTableId: 'stock-transfer-history-table',
        }}
        getColumns={getColumns}
        fetchDataFn={useStockTransferDataTable}
        defaultPageSize={20}
        exportConfig={{
          entityName: 'stock-transfers',
          columnMapping: {},
          columnWidths: [],
          headers: [],
        }}
        idField="id"
      />
      <StockTransferDetailDialog transferId={viewId} onClose={() => setViewId(null)} />
    </>
  )
}
