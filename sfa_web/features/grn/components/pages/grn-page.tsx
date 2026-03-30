'use client'

import { useState } from 'react'
import { CheckCircle, Trash2 } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { ScrollArea } from '@/components/ui/scroll-area'
import { Skeleton } from '@/components/ui/skeleton'
import { GrnConfirmDialog } from '../dialogs/grn-confirm-dialog'
import { GrnDeleteDialog } from '../dialogs/grn-delete-dialog'
import { useGrns } from '../../hooks/grn.hooks'
import { useConfirmDialog, useDeleteDialog } from '../../store'
import type { GrnStatus } from '../../schema/grn.schema'

// ── Badge helper ───────────────────────────────────────────────────────────

function GrnStatusBadge({ status }: { status: GrnStatus }) {
  if (status === 'Confirmed') {
    return <Badge variant="default" className="bg-green-600 hover:bg-green-700 text-xs">Confirmed</Badge>
  }
  if (status === 'Disputed') {
    return <Badge variant="destructive" className="text-xs">Disputed</Badge>
  }
  return <Badge variant="outline" className="text-xs">Pending</Badge>
}

// ── Page Component ─────────────────────────────────────────────────────────

export function GrnPage() {
  const [page, setPage] = useState(1)
  const [status, setStatus] = useState<string>('')
  const { open: openConfirm } = useConfirmDialog()
  const { open: openDelete } = useDeleteDialog()

  const pageSize = 20
  const { data, isLoading, isFetching } = useGrns(page, pageSize, status || undefined)

  const grns = data?.grns ?? []
  const totalCount = data?.totalCount ?? 0
  const totalPages = Math.ceil(totalCount / pageSize)

  function handleStatusChange(val: string) {
    setStatus(val === 'all' ? '' : val)
    setPage(1)
  }

  return (
    <div className="flex flex-col gap-6 p-6">
      {/* Header */}
      <div className="flex items-center justify-between bg-muted/90 p-10 rounded-lg">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Goods Received Notes</h1>
          <p className="text-muted-foreground">
            Track and confirm delivery of sales invoices
          </p>
        </div>
      </div>

      {/* Filters */}
      <div className="flex items-center gap-3 flex-wrap">
        <Select value={status || 'all'} onValueChange={handleStatusChange}>
          <SelectTrigger className="w-44">
            <SelectValue placeholder="All statuses" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">All Statuses</SelectItem>
            <SelectItem value="Pending">Pending</SelectItem>
            <SelectItem value="Confirmed">Confirmed</SelectItem>
            <SelectItem value="Disputed">Disputed</SelectItem>
          </SelectContent>
        </Select>

        {totalCount > 0 && (
          <span className="text-sm text-muted-foreground ml-auto">
            {totalCount} GRN{totalCount !== 1 ? 's' : ''}
          </span>
        )}
      </div>

      {/* Table */}
      <div className="rounded-md border">
        <ScrollArea className="h-[calc(100vh-340px)]">
          <table className="w-full text-sm">
            <thead className="sticky top-0 bg-muted/80 backdrop-blur z-10">
              <tr>
                <th className="text-left px-4 py-3 font-medium">GRN Number</th>
                <th className="text-left px-4 py-3 font-medium">Invoice Bill No</th>
                <th className="text-left px-4 py-3 font-medium">Distributor</th>
                <th className="text-left px-4 py-3 font-medium">Status</th>
                <th className="text-left px-4 py-3 font-medium">Received At</th>
                <th className="text-left px-4 py-3 font-medium">Confirmed By</th>
                <th className="text-left px-4 py-3 font-medium">Created</th>
                <th className="text-right px-4 py-3 font-medium">Actions</th>
              </tr>
            </thead>
            <tbody>
              {isLoading ? (
                Array.from({ length: 8 }).map((_, i) => (
                  <tr key={i} className="border-t">
                    {Array.from({ length: 8 }).map((_, j) => (
                      <td key={j} className="px-4 py-3">
                        <Skeleton className="h-4 w-full" />
                      </td>
                    ))}
                  </tr>
                ))
              ) : grns.length === 0 ? (
                <tr>
                  <td colSpan={8} className="px-4 py-16 text-center text-muted-foreground">
                    {status ? 'No GRNs match your filter.' : 'No GRNs found.'}
                  </td>
                </tr>
              ) : (
                grns.map((grn) => (
                  <tr key={grn.id} className="border-t hover:bg-muted/40 transition-colors">
                    <td className="px-4 py-3 font-medium">{grn.grnNumber}</td>
                    <td className="px-4 py-3 text-muted-foreground">{grn.salesInvoiceVchBillNo}</td>
                    <td className="px-4 py-3">{grn.distributorName}</td>
                    <td className="px-4 py-3">
                      <GrnStatusBadge status={grn.status} />
                    </td>
                    <td className="px-4 py-3 text-muted-foreground">
                      {grn.receivedAt
                        ? new Date(grn.receivedAt).toLocaleDateString()
                        : '—'}
                    </td>
                    <td className="px-4 py-3 text-muted-foreground">
                      {grn.confirmedByName ?? '—'}
                    </td>
                    <td className="px-4 py-3 text-muted-foreground text-xs">
                      {new Date(grn.createdAt).toLocaleDateString()}
                    </td>
                    <td className="px-4 py-3 text-right">
                      <div className="flex items-center justify-end gap-2">
                        {grn.status === 'Pending' && (
                          <Button
                            variant="outline"
                            size="sm"
                            className="text-green-700 border-green-300 hover:bg-green-50"
                            onClick={() => openConfirm(grn.id)}
                          >
                            <CheckCircle className="mr-1 h-3.5 w-3.5" />
                            Confirm
                          </Button>
                        )}
                        <Button
                          variant="ghost"
                          size="sm"
                          className="text-destructive hover:text-destructive hover:bg-destructive/10"
                          onClick={() => openDelete(grn.id)}
                        >
                          <Trash2 className="h-3.5 w-3.5" />
                        </Button>
                      </div>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </ScrollArea>

        {/* Pagination */}
        {totalPages > 1 && (
          <div className="flex items-center justify-between px-4 py-3 border-t bg-muted/20">
            <span className="text-sm text-muted-foreground">
              Page {page} of {totalPages}
            </span>
            <div className="flex gap-2">
              <Button
                variant="outline"
                size="sm"
                disabled={page <= 1 || isFetching}
                onClick={() => setPage((p) => p - 1)}
              >
                Previous
              </Button>
              <Button
                variant="outline"
                size="sm"
                disabled={page >= totalPages || isFetching}
                onClick={() => setPage((p) => p + 1)}
              >
                Next
              </Button>
            </div>
          </div>
        )}
      </div>

      <GrnConfirmDialog />
      <GrnDeleteDialog />
    </div>
  )
}
