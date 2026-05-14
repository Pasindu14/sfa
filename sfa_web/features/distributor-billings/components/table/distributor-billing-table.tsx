'use client'

import { useCallback, useState } from 'react'
import { DataTable } from '@/components/data-table/data-table'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { Button } from '@/components/ui/button'
import { RotateCcw } from 'lucide-react'
import { CalendarDatePicker } from '@/components/calendar-date-picker'
import { getDistributorBillingColumns } from '../columns/distributor-billing-columns'
import { useMyBillingsDataTable } from '../../hooks/distributor-billing.hooks'
import { DistributorBillingDetailDialog } from '../dialogs/distributor-billing-detail-dialog'

function toLocalDateStr(date: Date) {
  return [
    date.getFullYear(),
    String(date.getMonth() + 1).padStart(2, '0'),
    String(date.getDate()).padStart(2, '0'),
  ].join('-')
}

const today = toLocalDateStr(new Date())

export function DistributorBillingTable() {
  const [selectedId, setSelectedId] = useState<number | null>(null)

  const getColumns = useCallback(
    () => getDistributorBillingColumns((id) => setSelectedId(id)),
    [],
  )

  return (
    <>
      <DataTable
        customFilters={{ dateFrom: today, dateTo: today }}
        config={{
          enableRowSelection: false,
          enableSearch: true,
          enableDateFilter: false,
          enableExport: false,
          enableColumnResizing: false,
          enableUrlState: false,
          columnResizingTableId: 'distributor-billing-table',
          searchPlaceholder: 'Search by billing number or outlet...',
        }}
        getColumns={getColumns}
        fetchDataFn={useMyBillingsDataTable}
        defaultPageSize={20}
        exportConfig={{
          entityName: 'my-billings',
          columnMapping: {
            billingNumber: 'Billing No',
            outletName: 'Outlet',
            salesRepName: 'Sales Rep',
            totalAmount: 'Total Amount',
            status: 'Status',
          },
          columnWidths: [{ wch: 16 }, { wch: 30 }, { wch: 20 }, { wch: 14 }, { wch: 12 }],
          headers: ['Billing No', 'Outlet', 'Sales Rep', 'Total Amount', 'Status'],
        }}
        idField="id"
        renderCustomFilters={(filters, setFilters) => {
          const fromDate = filters?.dateFrom ? new Date(filters.dateFrom as string) : undefined
          const toDate = filters?.dateTo ? new Date(filters.dateTo as string) : undefined
          const hasActiveFilters = !!(filters?.status || filters?.dateFrom || filters?.dateTo)

          return (
            <div className="flex items-center gap-2">
              <CalendarDatePicker
                id="billing-date-range"
                date={{ from: fromDate, to: toDate }}
                onDateSelect={({ from, to }) =>
                  setFilters({
                    ...filters,
                    dateFrom: toLocalDateStr(from),
                    dateTo: toLocalDateStr(to),
                  })
                }
                numberOfMonths={2}
                variant="outline"
                className="w-fit cursor-pointer"
                closeOnSelect
              />

              <Select
                value={(filters?.status as string) ?? 'all'}
                onValueChange={(value) =>
                  setFilters({ ...filters, status: value === 'all' ? '' : value })
                }
              >
                <SelectTrigger className="h-8 w-36">
                  <SelectValue placeholder="All Statuses" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="all">All Statuses</SelectItem>
                  <SelectItem value="Submitted">Submitted</SelectItem>
                  <SelectItem value="Approved">Approved</SelectItem>
                  <SelectItem value="Cancelled">Cancelled</SelectItem>
                </SelectContent>
              </Select>

              {hasActiveFilters && (
                <Button
                  variant="ghost"
                  size="sm"
                  className="h-8 gap-1.5 text-muted-foreground"
                  onClick={() => setFilters({ status: '', dateFrom: '', dateTo: '' })}
                >
                  <RotateCcw className="h-3.5 w-3.5" />
                  Reset
                </Button>
              )}
            </div>
          )
        }}
      />
      <DistributorBillingDetailDialog
        id={selectedId}
        onClose={() => setSelectedId(null)}
      />
    </>
  )
}
