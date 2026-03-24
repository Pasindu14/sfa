'use server'

import { revalidatePath } from 'next/cache'
import { createAction } from '@/lib/actions/wrapper'
import client from '@/lib/api/client'
import type { ImportSalesInvoicesPayload, ImportBatchResult } from '../schema/sales-invoice.schema'

// Receives an already-parsed payload (parsing happens client-side for preview).
export const importSalesInvoicesAction = createAction(
  { name: 'importSalesInvoicesAction', requireAuth: true, requiredRole: 'Admin' },
  async (payload: ImportSalesInvoicesPayload) => {
    const res = await client.post('/api/v1/sales-invoices/import', payload)
    revalidatePath('/sales-invoices')
    return res.data.data as ImportBatchResult
  }
)
