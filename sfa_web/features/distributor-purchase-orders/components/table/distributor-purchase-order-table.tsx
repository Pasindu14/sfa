'use client'

import { useCallback } from 'react'
import { useRouter } from 'next/navigation'
import { Plus } from 'lucide-react'
import { DataTable } from '@/components/data-table/data-table'
import { Button } from '@/components/ui/button'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { getDistributorPurchaseOrderColumns } from '../columns/distributor-purchase-order-columns'
import { useMyPurchaseOrdersDataTable } from '../../hooks/distributor-purchase-order.hooks'

function toLocalDateStr(date: Date) {
  return [
    date.getFullYear(),
    String(date.getMonth() + 1).padStart(2, '0'),
    String(date.getDate()).padStart(2, '0'),
  ].join('-')
}

function getCurrentMonthDateRange() {
  const now = new Date()
  const firstDay = new Date(now.getFullYear(), now.getMonth(), 1)
  const lastDay = new Date(now.getFullYear(), now.getMonth() + 1, 0)
  return {
    from_date: toLocalDateStr(firstDay),
    to_date: toLocalDateStr(lastDay),
  }
}

export function DistributorPurchaseOrderTable() {
  const router = useRouter()
  const getColumns = useCallback(() => getDistributorPurchaseOrderColumns(), [])

  return (
    <div className="[&_th]:border-r [&_td]:border-r [&_th:last-child]:border-r-0 [&_td:last-child]:border-r-0">
    <DataTable
      config={{
        enableRowSelection: false,
        enableSearch: true,
        enableDateFilter: true,
        enableExport: false,
        enableColumnResizing: false,
        enableUrlState: false,
        columnResizingTableId: 'distributor-purchase-orders-table',
        searchPlaceholder: 'Search by order number...',
        defaultDateRange: getCurrentMonthDateRange(),
      }}
      getColumns={getColumns}
      fetchDataFn={useMyPurchaseOrdersDataTable}
      defaultPageSize={20}
      exportConfig={{
        entityName: 'purchase-orders',
        columnMapping: {
          orderNumber: 'Order #',
          status: 'Status',
          totalAmount: 'Total Amount',
          itemCount: 'Items',
          createdAt: 'Created',
        },
      }}
      idField="id"
      renderToolbarContent={() => (
        <Button
          onClick={() => router.push('/distributor-purchase-orders/new')}
          className="gap-2"
        >
          <Plus className="h-4 w-4" />
          New Order
        </Button>
      )}
      renderCustomFilters={(filters, setFilters) => (
        <Select
          value={(filters?.status as string) ?? 'all'}
          onValueChange={(value) =>
            setFilters({ ...filters, status: value === 'all' ? '' : value })
          }
        >
          <SelectTrigger className="h-8 w-36 sm:w-48">
            <SelectValue placeholder="All Status" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">All Status</SelectItem>
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
    />
    </div>
  )
}
