'use client'

import { useCallback } from 'react'
import { useRouter } from 'next/navigation'
import { DataTable } from '@/components/data-table/data-table'
import { Button } from '@/components/ui/button'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { Plus } from 'lucide-react'
import { useSalesOrderDataTable } from '../../hooks/sales-order.hooks'
import { getSalesOrderColumns } from '../columns/sales-order-columns'

export function SalesOrderTable() {
  const router = useRouter()
  const getColumns = useCallback(() => getSalesOrderColumns(), [])

  return (
    <DataTable
      config={{
        enableRowSelection: false,
        enableSearch: true,
        enableDateFilter: true,
        enableExport: false,
        enableColumnResizing: false,
        enableUrlState: false,
        columnResizingTableId: 'sales-orders-table',
        searchPlaceholder: 'Search orders...',
      }}
      getColumns={getColumns}
      fetchDataFn={useSalesOrderDataTable}
      exportConfig={{
        entityName: 'sales-orders',
        columnMapping: {
          orderNumber: 'Order #',
          distributorName: 'Distributor',
          status: 'Status',
          totalAmount: 'Total',
          createdAt: 'Created At',
        },
        columnWidths: [{ wch: 15 }, { wch: 25 }, { wch: 25 }, { wch: 15 }, { wch: 20 }],
        headers: ['Order #', 'Distributor', 'Status', 'Total', 'Created At'],
      }}
      idField="id"
      renderCustomFilters={(filters, setFilters) => (
        <Select
          value={filters?.status ?? ''}
          onValueChange={(value) =>
            setFilters({ ...filters, status: value === 'all' ? '' : value })
          }
        >
          <SelectTrigger className="h-8 w-44">
            <SelectValue placeholder="All Statuses" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">All Statuses</SelectItem>
            <SelectItem value="Draft">Draft</SelectItem>
            <SelectItem value="PendingRepApproval">Pending Rep Approval</SelectItem>
            <SelectItem value="PendingManagerApproval">Pending Manager Approval</SelectItem>
            <SelectItem value="PendingDistributorFinalization">Pending Finalization</SelectItem>
            <SelectItem value="PendingDistributorAcknowledgement">Pending Acknowledgement</SelectItem>
            <SelectItem value="Finalized">Finalized</SelectItem>
            <SelectItem value="Cancelled">Cancelled</SelectItem>
          </SelectContent>
        </Select>
      )}
      renderToolbarContent={() => (
        <Button onClick={() => router.push('/sales-orders/new')} className="gap-2">
          <Plus className="h-4 w-4" />
          New Order
        </Button>
      )}
    />
  )
}
