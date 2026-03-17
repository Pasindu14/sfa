'use client'

import { useCallback } from 'react'
import { useSession } from 'next-auth/react'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { DataTable } from '@/components/data-table/data-table'
import { useSalesOrderDataTable } from '../../hooks/sales-order.hooks'
import { useSalesOrderFilters } from '../../store'
import { getColumns } from '../columns/sales-order-columns'

const STATUS_OPTIONS = [
  { value: '', label: 'All Statuses' },
  { value: 'Draft', label: 'Draft' },
  { value: 'PendingRepApproval', label: 'Pending Rep Approval' },
  { value: 'PendingManagerApproval', label: 'Pending Manager Approval' },
  { value: 'PendingDistributorFinalization', label: 'Pending Finalization' },
  { value: 'PendingDistributorAcknowledgement', label: 'Pending Acknowledgement' },
  { value: 'Finalized', label: 'Finalized' },
  { value: 'Cancelled', label: 'Cancelled' },
]

export function SalesOrderTable() {
  const { data: session } = useSession()
  const role = session?.user?.role ?? ''
  const filters = useSalesOrderFilters()

  const getColumnsCallback = useCallback(
    () => getColumns(role),
    [role],
  )

  return (
    <DataTable
      config={{
        enableRowSelection: false,
        enableSearch: true,
        enableDateFilter: true,
        enableColumnVisibility: true,
        enableExport: false,
        enableColumnResizing: false,
        enableUrlState: false,
        columnResizingTableId: 'sales-orders-table',
        searchPlaceholder: 'Search orders...',
      }}
      getColumns={getColumnsCallback}
      fetchDataFn={useSalesOrderDataTable}
      exportConfig={{
        entityName: 'sales-orders',
        columnMapping: {
          orderNumber: 'Order #',
          distributorName: 'Distributor',
          status: 'Status',
          totalAmount: 'Total',
          submittedAt: 'Submitted At',
          createdAt: 'Created At',
        },
        columnWidths: [
          { wch: 18 },
          { wch: 25 },
          { wch: 20 },
          { wch: 15 },
          { wch: 20 },
          { wch: 20 },
        ],
        headers: [
          'Order #',
          'Distributor',
          'Status',
          'Total',
          'Submitted At',
          'Created At',
        ],
      }}
      idField="id"
      renderCustomFilters={(_, setFilters) => (
        <Select
          value={filters.status}
          onValueChange={(val) => {
            filters.setStatus(val)
            setFilters((prev: Record<string, unknown>) => ({ ...prev, status: val }))
          }}
        >
          <SelectTrigger className="w-[220px]">
            <SelectValue placeholder="All Statuses" />
          </SelectTrigger>
          <SelectContent>
            {STATUS_OPTIONS.map((opt) => (
              <SelectItem key={opt.value} value={opt.value}>
                {opt.label}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
      )}
    />
  )
}
