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
import { DistributorBillingReviewDialog } from '../dialogs/distributor-billing-review-dialog'
import { DistributorBillingCashCollectedDialog } from '../dialogs/distributor-billing-cash-collected-dialog'
import type { DistributorBillingListItem } from '../../schema/distributor-billing.schema'
import { toColomboDateStr } from '@/lib/utils/datetime'

function toLocalDateStr(date: Date) {
  return toColomboDateStr(date)
}

const today = toLocalDateStr(new Date())
const thisMonthStart = toLocalDateStr(new Date(new Date().getFullYear(), new Date().getMonth(), 1))

export function DistributorBillingTable() {
  const [selectedId, setSelectedId] = useState<number | null>(null)
  const [reviewBilling, setReviewBilling] = useState<DistributorBillingListItem | null>(null)
  const [cashCollectedBilling, setCashCollectedBilling] = useState<DistributorBillingListItem | null>(null)

  const getColumns = useCallback(
    () => getDistributorBillingColumns(
      (id) => setSelectedId(id),
      (billing) => setReviewBilling(billing),
      (billing) => setCashCollectedBilling(billing),
    ),
    [],
  )

  return (
    <>
      <DataTable
        customFilters={{ dateFrom: thisMonthStart, dateTo: today }}
        config={{
          enableRowSelection: false,
          enableSearch: true,
          enableDateFilter: false,
          enableExport: false,
          enableColumnResizing: true,
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
            repStatus: 'Rep Status',
            distributorStatus: 'Distributor Status',
          },
          columnWidths: [{ wch: 16 }, { wch: 30 }, { wch: 20 }, { wch: 14 }, { wch: 12 }, { wch: 14 }],
          headers: ['Billing No', 'Outlet', 'Sales Rep', 'Total Amount', 'Rep Status', 'Distributor Status'],
        }}
        idField="id"
        renderCustomFilters={(filters, setFilters) => {
          const fromDate = filters?.dateFrom ? new Date(filters.dateFrom as string) : undefined
          const toDate = filters?.dateTo ? new Date(filters.dateTo as string) : undefined
          const hasActiveFilters = !!(filters?.distributorStatus || filters?.dateFrom || filters?.dateTo || filters?.paymentType || filters?.isCashCollected)

          return (
            <div className="flex flex-wrap items-center gap-2">
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
              />

              <Select
                value={(filters?.distributorStatus as string) ?? 'all'}
                onValueChange={(value) =>
                  setFilters({ ...filters, distributorStatus: value === 'all' ? '' : value })
                }
              >
                <SelectTrigger className="h-8 w-36 sm:w-40">
                  <SelectValue placeholder="Distributor Status" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="all">All Distributor Status</SelectItem>
                  <SelectItem value="Pending">Pending</SelectItem>
                  <SelectItem value="Approved">Approved</SelectItem>
                  <SelectItem value="Rejected">Rejected</SelectItem>
                </SelectContent>
              </Select>

              <Select
                value={(filters?.paymentType as string) ?? 'all'}
                onValueChange={(value) =>
                  setFilters({ ...filters, paymentType: value === 'all' ? '' : value })
                }
              >
                <SelectTrigger className="h-8 w-28 sm:w-32">
                  <SelectValue placeholder="Payment" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="all">All Payments</SelectItem>
                  <SelectItem value="Cash">Cash</SelectItem>
                  <SelectItem value="Credit">Credit</SelectItem>
                </SelectContent>
              </Select>

              <Select
                value={(filters?.isCashCollected as string) ?? 'all'}
                onValueChange={(value) =>
                  setFilters({ ...filters, isCashCollected: value === 'all' ? '' : value })
                }
              >
                <SelectTrigger className="h-8 w-36 sm:w-40">
                  <SelectValue placeholder="Cash Collected" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="all">All Collections</SelectItem>
                  <SelectItem value="true">Collected</SelectItem>
                  <SelectItem value="false">Not Collected</SelectItem>
                </SelectContent>
              </Select>

              {hasActiveFilters && (
                <Button
                  variant="ghost"
                  size="sm"
                  className="h-8 gap-1.5 text-muted-foreground"
                  onClick={() => setFilters({ distributorStatus: '', dateFrom: '', dateTo: '', paymentType: '', isCashCollected: '' })}
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
      <DistributorBillingReviewDialog
        billing={reviewBilling}
        onClose={() => setReviewBilling(null)}
      />
      <DistributorBillingCashCollectedDialog
        billing={cashCollectedBilling}
        onClose={() => setCashCollectedBilling(null)}
      />
    </>
  )
}
