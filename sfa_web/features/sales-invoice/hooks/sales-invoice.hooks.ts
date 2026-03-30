'use client'

import { useState } from 'react'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import { handleErrorToast } from '@/lib/hooks/use-error-toast'
import { importSalesInvoicesAction, deleteSalesInvoiceAction } from '../actions/sales-invoice.actions'
import { getSalesInvoicesAction, getSalesInvoiceByIdAction } from '../actions/sales-invoice-list.actions'
import { useImportDialog, useDeleteDialog, useSalesInvoiceFilterStore } from '../store'
import type { ImportBatchResult, ImportSalesInvoicesPayload } from '../schema/sales-invoice.schema'
import type { SalesInvoiceListItem } from '../schema/sales-invoice-list.schema'

// ── Query key factory ──────────────────────────────────────────────────────

export const salesInvoiceKeys = {
  all: ['sales-invoices'] as const,
  lists: () => [...salesInvoiceKeys.all, 'list'] as const,
  list: (filters: object) => [...salesInvoiceKeys.lists(), filters] as const,
  details: () => [...salesInvoiceKeys.all, 'detail'] as const,
  detail: (id: number) => [...salesInvoiceKeys.details(), id] as const,
}

// ── Detail query hook ──────────────────────────────────────────────────────

export function useSalesInvoiceDetail(id: number | null) {
  return useQuery({
    queryKey: salesInvoiceKeys.detail(id!),
    queryFn: async () => {
      const result = await getSalesInvoiceByIdAction(id!)
      if (!result.success) throw new Error(result.error)
      return result.data
    },
    enabled: id !== null,
  })
}

// ── DataTable hook (fetchDataFn with isQueryHook = true) ───────────────────

export function useSalesInvoiceDataTable(
  page: number,
  pageSize: number,
  search: string,
  _dateRange?: { from_date: string; to_date: string },
  _sortBy?: string,
  _sortOrder?: string,
  _caseConfig?: unknown,
  customFilters?: { status?: string },
) {
  // Direct selector — no useShallow needed for a single value
  const appliedFilters = useSalesInvoiceFilterStore((s) => s.appliedFilters)

  return useQuery({
    queryKey: salesInvoiceKeys.list({ page, pageSize, search, customFilters, appliedFilters }),
    queryFn: async () => {
      const result = await getSalesInvoicesAction(
        page,
        pageSize,
        search || undefined,
        customFilters?.status || undefined,
        appliedFilters?.date,
        appliedFilters?.distributorId ?? undefined,
      )
      if (!result.success) throw new Error(result.error)
      const { invoices, totalCount, page: p, pageSize: ps } = result.data
      return {
        success: true as const,
        data: invoices as SalesInvoiceListItem[],
        pagination: {
          page: p,
          limit: ps,
          total_pages: Math.ceil(totalCount / ps),
          total_items: totalCount,
        },
      }
    },
    // No keepPreviousData — we want the table to show a fresh loading state
    // when the user changes date/distributor and clicks Reload, not stale rows.
  })
}

;(useSalesInvoiceDataTable as unknown as Record<string, unknown>).isQueryHook = true

// ── Delete mutation hook ───────────────────────────────────────────────────

export function useDeleteSalesInvoice() {
  const queryClient = useQueryClient()
  const { close } = useDeleteDialog()

  return useMutation({
    mutationFn: async (id: number) => {
      const result = await deleteSalesInvoiceAction(id)
      if (!result.success) throw result
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: salesInvoiceKeys.all })
      close()
      toast.success('Sales invoice deleted successfully')
    },
    onError: (error: unknown) => {
      handleErrorToast(error as any, 'sales invoice', 'delete')
    },
  })
}

// ── Import mutation hook ───────────────────────────────────────────────────

export function useImportSalesInvoices() {
  const { close } = useImportDialog()
  const [batchResult, setBatchResult] = useState<ImportBatchResult | null>(null)

  const mutation = useMutation({
    mutationFn: async (payload: ImportSalesInvoicesPayload) => {
      const result = await importSalesInvoicesAction(payload)
      if (!result.success) throw result
      return result.data
    },
    onSuccess: (data) => {
      setBatchResult(data)
      if (data.skippedInvoices === 0) {
        toast.success(`Imported ${data.importedInvoices} invoices (${data.batchNumber})`)
      } else if (data.importedInvoices > 0) {
        toast.warning(`Imported ${data.importedInvoices}/${data.totalInvoices} — ${data.skippedInvoices} skipped`)
      } else {
        toast.error('Import failed — no invoices were imported')
      }
    },
    onError: (error: unknown) => {
      handleErrorToast(error as any, 'sales invoice', 'import')
    },
  })

  function reset() {
    setBatchResult(null)
    mutation.reset()
  }

  return {
    ...mutation,
    batchResult,
    reset,
    closeAndReset: () => { reset(); close() },
  }
}
