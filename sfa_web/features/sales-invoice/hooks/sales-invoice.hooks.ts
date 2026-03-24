'use client'

import { useState } from 'react'
import { useMutation } from '@tanstack/react-query'
import { toast } from 'sonner'
import { handleErrorToast } from '@/lib/hooks/use-error-toast'
import { importSalesInvoicesAction } from '../actions/sales-invoice.actions'
import { useImportDialog } from '../store'
import type { ImportBatchResult, ImportSalesInvoicesPayload } from '../schema/sales-invoice.schema'

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
