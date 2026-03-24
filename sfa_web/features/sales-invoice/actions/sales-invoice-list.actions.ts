'use server'

import { createAction } from '@/lib/actions/wrapper'
import client from '@/lib/api/client'
import type { SalesInvoiceListResponse, SalesInvoiceDetail } from '../schema/sales-invoice-list.schema'

export const getSalesInvoicesAction = createAction(
  { name: 'getSalesInvoicesAction', requireAuth: true, requiredRole: 'Admin' },
  async (
    page: number = 1,
    pageSize: number = 20,
    search?: string,
    status?: string,
    date?: string,
    distributorId?: number,
  ) => {
    const res = await client.get('/api/v1/sales-invoices', {
      params: {
        page,
        pageSize,
        search: search || undefined,
        status: status || undefined,
        date: date || undefined,
        distributorId: distributorId || undefined,
      },
    })
    // API returns { data: SalesInvoiceListDto[], pagination: { ... } }
    const body = res.data
    return {
      invoices: body.data as SalesInvoiceListResponse['invoices'],
      totalCount: body.pagination?.totalCount ?? 0,
      page: body.pagination?.page ?? page,
      pageSize: body.pagination?.pageSize ?? pageSize,
    } satisfies SalesInvoiceListResponse
  }
)

export const getSalesInvoiceByIdAction = createAction(
  { name: 'getSalesInvoiceByIdAction', requireAuth: true, requiredRole: 'Admin' },
  async (id: number) => {
    const res = await client.get(`/api/v1/sales-invoices/${id}`)
    return res.data.data as SalesInvoiceDetail
  }
)
