'use client'

import { Upload } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { SalesInvoiceImportDialog } from '../dialogs/sales-invoice-import-dialog'
import { useImportDialog } from '../../store'

export function SalesInvoiceImportPage() {
  const { open } = useImportDialog()

  return (
    <div className="flex flex-col gap-6 p-6">
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

      {/* Invoice list table will be added in Step 10 */}
      <div className="flex items-center justify-center rounded-lg border border-dashed py-24 text-sm text-muted-foreground">
        No invoices yet — use Import Excel to load invoices from BUSY ERP.
      </div>

      <SalesInvoiceImportDialog />
    </div>
  )
}
