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
import { getStockColumns } from '@/features/stock/components/columns/stock-columns'
import { useMyStockDataTable } from '../../hooks/distributor-stock.hooks'

export function DistributorStockTable() {
  const getColumns = useCallback(() => getStockColumns(), [])

  return (
    <DataTable
      config={{
        enableRowSelection: false,
        enableSearch: true,
        enableDateFilter: false,
        enableExport: false,
        enableColumnResizing: true,
        enableUrlState: false,
        columnResizingTableId: 'distributor-stock-table',
        searchPlaceholder: 'Search by code or description...',
      }}
      getColumns={getColumns}
      fetchDataFn={useMyStockDataTable}
      defaultPageSize={50}
      exportConfig={{
        entityName: 'my-stock',
        columnMapping: {
          productCode: 'Product Code',
          productDescription: 'Description',
          quantityOnHand: 'Qty on Hand',
          lastUpdatedAt: 'Last Updated',
        },
        columnWidths: [{ wch: 15 }, { wch: 35 }, { wch: 12 }, { wch: 20 }],
        headers: ['Product Code', 'Description', 'Qty on Hand', 'Last Updated'],
      }}
      idField="id"
      renderCustomFilters={(filters, setFilters) => (
        <Select
          value={(filters?.stockType as string) ?? 'all'}
          onValueChange={(value) =>
            setFilters({ ...filters, stockType: value === 'all' ? '' : value })
          }
        >
          <SelectTrigger className="h-8 w-36">
            <SelectValue placeholder="All Types" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">All Types</SelectItem>
            <SelectItem value="Normal">Normal</SelectItem>
            <SelectItem value="FreeIssue">Free Issue</SelectItem>
          </SelectContent>
        </Select>
      )}
    />
  )
}
