'use client'

import { useCallback, useState } from 'react'
import Link from 'next/link'
import { useParams, useSearchParams } from 'next/navigation'
import { ArrowLeft, RotateCcw } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { DataTable } from '@/components/data-table/data-table'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { getDistributorBillingColumns } from '@/features/distributor-billings/components/columns/distributor-billing-columns'
import { useMyBillingsDataTable } from '@/features/distributor-billings/hooks/distributor-billing.hooks'
import { DistributorBillingDetailDialog } from '@/features/distributor-billings/components/dialogs/distributor-billing-detail-dialog'
import { DistributorBillingReviewDialog } from '@/features/distributor-billings/components/dialogs/distributor-billing-review-dialog'
import { DistributorBillingCashCollectedDialog } from '@/features/distributor-billings/components/dialogs/distributor-billing-cash-collected-dialog'
import type { DistributorBillingListItem } from '@/features/distributor-billings/schema/distributor-billing.schema'

export function OutletBillsPage() {
  const params = useParams()
  const searchParams = useSearchParams()
  const outletId = Number(params.id)
  const outletName = searchParams.get('name') ?? `Outlet #${outletId}`

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
    <div className="flex flex-col gap-6 p-6">

      <div>
        <Link href="/portal/outlets">
          <Button variant="ghost" size="sm" className="gap-1.5 -ml-2">
            <ArrowLeft className="h-4 w-4" />
            My Outlets
          </Button>
        </Link>
      </div>

      <div className="bg-muted/90 p-10 rounded-lg">
        <h1 className="text-3xl font-bold tracking-tight">Bills</h1>
        <p className="text-muted-foreground">{outletName}</p>
      </div>

      <DataTable
        customFilters={{ outletId }}
        config={{
          enableRowSelection: false,
          enableSearch: true,
          enableDateFilter: false,
          enableExport: false,
          enableColumnResizing: false,
          enableUrlState: false,
          columnResizingTableId: `outlet-bills-table-${outletId}`,
          searchPlaceholder: 'Search by billing number...',
        }}
        getColumns={getColumns}
        fetchDataFn={useMyBillingsDataTable}
        defaultPageSize={20}
        exportConfig={{
          entityName: 'outlet-bills',
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
          const hasActiveFilters = !!(filters?.distributorStatus || filters?.paymentType || filters?.isCashCollected)
          return (
            <div className="flex flex-wrap items-center gap-2">
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
                  onClick={() => setFilters({ ...filters, distributorStatus: '', paymentType: '', isCashCollected: '' })}
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
    </div>
  )
}
