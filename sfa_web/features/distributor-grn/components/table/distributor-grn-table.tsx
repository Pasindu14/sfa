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
import { getDistributorGrnColumns } from '../columns/distributor-grn-columns'
import { useMyGrnsDataTable } from '../../hooks/distributor-grn.hooks'
import { DistributorGrnConfirmDialog } from '../dialogs/distributor-grn-confirm-dialog'
import { DistributorGrnDetailDialog } from '../dialogs/distributor-grn-detail-dialog'
import { toColomboDateStr } from '@/lib/utils/datetime'

function toLocalDateStr(date: Date) {
  return toColomboDateStr(date)
}

const today = toLocalDateStr(new Date())

export function DistributorGrnTable() {
  const [confirmGrnId, setConfirmGrnId] = useState<number | null>(null)
  const [viewGrnId, setViewGrnId] = useState<number | null>(null)

  const getColumns = useCallback(
    () => getDistributorGrnColumns(
      (id) => setConfirmGrnId(id),
      (id) => setViewGrnId(id),
    ),
    [],
  )

  return (
    <>
      <DataTable
        config={{
          enableRowSelection: false,
          enableSearch: true,
          enableDateFilter: true,
          enableExport: false,
          enableColumnResizing: true,
          enableUrlState: false,
          columnResizingTableId: 'distributor-grn-table',
          defaultDateRange: { from_date: today, to_date: today },
        }}
        getColumns={getColumns}
        fetchDataFn={useMyGrnsDataTable}
        defaultPageSize={20}
        exportConfig={{
          entityName: 'my-grns',
          columnMapping: {
            grnNumber: 'GRN Number',
            salesInvoiceVchBillNo: 'Invoice No',
            status: 'Status',
            receivedAt: 'Received At',
            confirmedByName: 'Confirmed By',
            createdAt: 'Created',
          },
          columnWidths: [{ wch: 16 }, { wch: 16 }, { wch: 12 }, { wch: 14 }, { wch: 20 }, { wch: 14 }],
          headers: ['GRN Number', 'Invoice No', 'Status', 'Received At', 'Confirmed By', 'Created'],
        }}
        idField="id"
        renderCustomFilters={(filters, setFilters) => (
          <Select
            value={(filters?.status as string) ?? 'all'}
            onValueChange={(value) =>
              setFilters({ ...filters, status: value === 'all' ? '' : value })
            }
          >
            <SelectTrigger className="h-8 w-32 sm:w-36">
              <SelectValue placeholder="Status" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">All Status</SelectItem>
              <SelectItem value="Pending">Pending</SelectItem>
              <SelectItem value="Confirmed">Confirmed</SelectItem>
              <SelectItem value="Disputed">Disputed</SelectItem>
            </SelectContent>
          </Select>
        )}
      />

      <DistributorGrnConfirmDialog
        grnId={confirmGrnId}
        onClose={() => setConfirmGrnId(null)}
      />

      <DistributorGrnDetailDialog
        id={viewGrnId}
        onClose={() => setViewGrnId(null)}
      />
    </>
  )
}
