'use client'

import { useCallback } from 'react'
import { Users } from 'lucide-react'
import { DataTable } from '@/components/data-table/data-table'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { useRepBillDataTable } from '../../hooks/rep-bill.hooks'
import { useRepBillDetailDialog, useRepBillFilters } from '../../store'
import { getRepBillColumns } from '../columns/rep-bill-columns'
import { RepBillFilterBar } from '../filters/rep-bill-filter-bar'

export function RepBillTable() {
  const { appliedFilters } = useRepBillFilters()
  const { open: openDetail } = useRepBillDetailDialog()

  const getColumns = useCallback(() => getRepBillColumns(openDetail), [openDetail])

  return (
    <div className="flex flex-col gap-4">
      <RepBillFilterBar />

      {appliedFilters ? (
        <DataTable
          // Remounting on commit is what carries the newly applied filters into the table:
          // `customFilters` is only an *initial* value, so a re-render cannot push them in.
          // It also resets paging, which is the right behaviour when the rep or range changes.
          key={`${appliedFilters.salesRepId}-${appliedFilters.dateFrom}-${appliedFilters.dateTo}`}
          config={{
            enableRowSelection: false,
            // GET /api/v1/billings has no `search` param (only the distributor portal endpoint
            // does), so a search box here would look functional and do nothing.
            enableSearch: false,
            // The date range lives in the filter bar above, not the toolbar picker.
            enableDateFilter: false,
            enableExport: true,
            enableColumnResizing: true,
            enableUrlState: false,
            columnResizingTableId: 'rep-bills-table',
          }}
          getColumns={getColumns}
          fetchDataFn={useRepBillDataTable}
          defaultPageSize={20}
          exportConfig={{
            entityName: 'rep-bills',
            columnMapping: {
              billingNumber: 'Bill No',
              billingDate: 'Billing Date',
              outletName: 'Outlet',
              distributorName: 'Distributor',
              salesRepName: 'Sales Rep',
              supervisorName: 'Supervisor',
              totalAmount: 'Total Amount',
              paymentType: 'Payment',
              repStatus: 'Rep Status',
              distributorStatus: 'Distributor Status',
            },
            columnWidths: [
              { wch: 16 },
              { wch: 14 },
              { wch: 30 },
              { wch: 24 },
              { wch: 20 },
              { wch: 20 },
              { wch: 14 },
              { wch: 10 },
              { wch: 12 },
              { wch: 16 },
            ],
            headers: [
              'Bill No',
              'Billing Date',
              'Outlet',
              'Distributor',
              'Sales Rep',
              'Supervisor',
              'Total Amount',
              'Payment',
              'Rep Status',
              'Distributor Status',
            ],
          }}
          idField="id"
          renderCustomFilters={(filters, setFilters) => (
            <div className="flex flex-wrap items-center gap-2">
              <Select
                value={(filters?.distributorStatus as string) || 'all'}
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
                value={(filters?.paymentType as string) || 'all'}
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
            </div>
          )}
        />
      ) : (
        <div className="flex flex-col items-center justify-center gap-2 rounded-lg border border-dashed py-16 text-center">
          <Users className="h-8 w-8 text-muted-foreground/40" />
          <p className="text-sm font-medium text-muted-foreground">
            Choose a supervisor and a sales rep, then press{' '}
            <span className="font-semibold">Load bills</span>
          </p>
          <p className="text-xs text-muted-foreground/60">
            The date range defaults to this month so far
          </p>
        </div>
      )}
    </div>
  )
}
