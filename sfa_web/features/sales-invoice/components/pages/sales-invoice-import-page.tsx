'use client'

import { useState } from 'react'
import { Upload, Search } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Badge } from '@/components/ui/badge'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { ScrollArea } from '@/components/ui/scroll-area'
import { Skeleton } from '@/components/ui/skeleton'
import { SalesInvoiceImportDialog } from '../dialogs/sales-invoice-import-dialog'
import { SalesInvoiceDetailDrawer } from '../dialogs/sales-invoice-detail-drawer'
import { useImportDialog } from '../../store'
import { useSalesInvoices } from '../../hooks/sales-invoice-list.hooks'
import type { SalesInvoiceListItem, SalesInvoiceStatus, SalesInvoiceType } from '../../schema/sales-invoice-list.schema'

// ── Badge helpers ──────────────────────────────────────────────────────────

function StatusBadge({ status }: { status: SalesInvoiceStatus }) {
  if (status === 'GrnReceived') {
    return <Badge variant="default" className="bg-green-600 hover:bg-green-700 text-xs">GRN Received</Badge>
  }
  if (status === 'Disputed') {
    return <Badge variant="destructive" className="text-xs">Disputed</Badge>
  }
  return <Badge variant="outline" className="text-xs">Pending</Badge>
}

function InvoiceTypeBadge({ type }: { type: SalesInvoiceType }) {
  if (type === 'FreeIssue') {
    return <Badge className="bg-amber-500 hover:bg-amber-600 text-white text-xs">Free Issue</Badge>
  }
  return <Badge variant="secondary" className="text-xs">Regular</Badge>
}

function formatCurrency(amount: number) {
  return new Intl.NumberFormat('en-LK', { style: 'currency', currency: 'LKR', minimumFractionDigits: 2 }).format(amount)
}

// ── Page Component ─────────────────────────────────────────────────────────

export function SalesInvoiceImportPage() {
  const { open } = useImportDialog()

  // List state
  const [page, setPage] = useState(1)
  const [search, setSearch] = useState('')
  const [searchInput, setSearchInput] = useState('')
  const [status, setStatus] = useState<string>('')

  // Drawer state
  const [selectedId, setSelectedId] = useState<number | null>(null)
  const [drawerOpen, setDrawerOpen] = useState(false)

  const pageSize = 20
  const { data, isLoading, isFetching } = useSalesInvoices(page, pageSize, search, status || undefined)

  const invoices = data?.invoices ?? []
  const totalCount = data?.totalCount ?? 0
  const totalPages = Math.ceil(totalCount / pageSize)

  function handleSearch(e: React.FormEvent) {
    e.preventDefault()
    setSearch(searchInput)
    setPage(1)
  }

  function handleRowClick(invoice: SalesInvoiceListItem) {
    setSelectedId(invoice.id)
    setDrawerOpen(true)
  }

  function handleStatusChange(val: string) {
    setStatus(val === 'all' ? '' : val)
    setPage(1)
  }

  return (
    <div className="flex flex-col gap-6 p-6">
      {/* Header */}
      <div className="flex items-center justify-between bg-muted/90 p-10 rounded-lg">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Sales Invoices</h1>
          <p className="text-muted-foreground">
            Import and manage BUSY ERP sales invoices
          </p>
        </div>
        <Button onClick={open}>
          <Upload className="mr-2 h-4 w-4" />
          Import Excel
        </Button>
      </div>

      {/* Filters */}
      <div className="flex items-center gap-3 flex-wrap">
        <form onSubmit={handleSearch} className="flex items-center gap-2">
          <div className="relative">
            <Search className="absolute left-2.5 top-2.5 h-4 w-4 text-muted-foreground" />
            <Input
              placeholder="Search by bill no, distributor..."
              className="pl-8 w-72"
              value={searchInput}
              onChange={(e) => setSearchInput(e.target.value)}
            />
          </div>
          <Button type="submit" variant="secondary" size="sm">Search</Button>
          {search && (
            <Button
              type="button"
              variant="ghost"
              size="sm"
              onClick={() => { setSearch(''); setSearchInput(''); setPage(1) }}
            >
              Clear
            </Button>
          )}
        </form>

        <Select value={status || 'all'} onValueChange={handleStatusChange}>
          <SelectTrigger className="w-44">
            <SelectValue placeholder="All statuses" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">All Statuses</SelectItem>
            <SelectItem value="Pending">Pending</SelectItem>
            <SelectItem value="GrnReceived">GRN Received</SelectItem>
            <SelectItem value="Disputed">Disputed</SelectItem>
          </SelectContent>
        </Select>

        {totalCount > 0 && (
          <span className="text-sm text-muted-foreground ml-auto">
            {totalCount} invoice{totalCount !== 1 ? 's' : ''}
          </span>
        )}
      </div>

      {/* Table */}
      <div className="rounded-md border">
        <ScrollArea className="h-[calc(100vh-360px)]">
          <table className="w-full text-sm">
            <thead className="sticky top-0 bg-muted/80 backdrop-blur z-10">
              <tr>
                <th className="text-left px-4 py-3 font-medium">Bill No</th>
                <th className="text-left px-4 py-3 font-medium">Distributor</th>
                <th className="text-left px-4 py-3 font-medium">Invoice Date</th>
                <th className="text-left px-4 py-3 font-medium">Type</th>
                <th className="text-right px-4 py-3 font-medium">Amount</th>
                <th className="text-left px-4 py-3 font-medium">Status</th>
                <th className="text-left px-4 py-3 font-medium">Batch</th>
              </tr>
            </thead>
            <tbody>
              {isLoading ? (
                Array.from({ length: 8 }).map((_, i) => (
                  <tr key={i} className="border-t">
                    {Array.from({ length: 7 }).map((_, j) => (
                      <td key={j} className="px-4 py-3">
                        <Skeleton className="h-4 w-full" />
                      </td>
                    ))}
                  </tr>
                ))
              ) : invoices.length === 0 ? (
                <tr>
                  <td colSpan={7} className="px-4 py-16 text-center text-muted-foreground">
                    {search || status
                      ? 'No invoices match your filters.'
                      : 'No invoices yet — use Import Excel to load invoices from BUSY ERP.'}
                  </td>
                </tr>
              ) : (
                invoices.map((invoice) => (
                  <tr
                    key={invoice.id}
                    className="border-t hover:bg-muted/40 cursor-pointer transition-colors"
                    onClick={() => handleRowClick(invoice)}
                  >
                    <td className="px-4 py-3 font-medium">{invoice.vchBillNo}</td>
                    <td className="px-4 py-3 text-muted-foreground">{invoice.distributorName}</td>
                    <td className="px-4 py-3">{new Date(invoice.invoiceDate).toLocaleDateString()}</td>
                    <td className="px-4 py-3">
                      <InvoiceTypeBadge type={invoice.invoiceType} />
                    </td>
                    <td className="px-4 py-3 text-right font-medium">{formatCurrency(invoice.totalAmount)}</td>
                    <td className="px-4 py-3">
                      <StatusBadge status={invoice.status} />
                    </td>
                    <td className="px-4 py-3 text-xs text-muted-foreground">{invoice.batchNumber}</td>
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

      <SalesInvoiceImportDialog />

      <SalesInvoiceDetailDrawer
        invoiceId={selectedId}
        open={drawerOpen}
        onClose={() => setDrawerOpen(false)}
      />
    </div>
  )
}
