'use client'

import { useQuery, keepPreviousData } from '@tanstack/react-query'
import { getSalesInvoicesAction, getSalesInvoiceByIdAction } from '../actions/sales-invoice-list.actions'

// ── Query key factory ──────────────────────────────────────────────────────

export const salesInvoiceKeys = {
  all: ['sales-invoices'] as const,
  lists: () => [...salesInvoiceKeys.all, 'list'] as const,
  list: (filters: object) => [...salesInvoiceKeys.lists(), filters] as const,
  details: () => [...salesInvoiceKeys.all, 'detail'] as const,
  detail: (id: number) => [...salesInvoiceKeys.details(), id] as const,
}

// ── List query hook ────────────────────────────────────────────────────────

export function useSalesInvoices(
  page: number,
  pageSize: number,
  search: string,
  status?: string,
) {
  return useQuery({
    queryKey: salesInvoiceKeys.list({ page, pageSize, search, status }),
    queryFn: async () => {
      const result = await getSalesInvoicesAction(page, pageSize, search || undefined, status || undefined)
      if (!result.success) throw new Error(result.error)
      return result.data
    },
    placeholderData: keepPreviousData,
  })
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
