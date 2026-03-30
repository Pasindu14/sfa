'use client'

import { SalesInvoiceTable } from '../table/sales-invoice-table'
import { SalesInvoiceImportDialog } from '../dialogs/sales-invoice-import-dialog'
import { SalesInvoiceDetailDialog } from '../dialogs/sales-invoice-detail-dialog'
import { DeleteSalesInvoiceDialog } from '../dialogs/sales-invoice-delete-dialog'

export function SalesInvoiceListPage() {
  return (
    <div className="flex flex-col gap-6 p-6">
      <div className="flex items-center justify-between bg-muted/90 p-10 rounded-lg">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Sales Invoices</h1>
          <p className="text-muted-foreground">Import and manage BUSY ERP sales invoices</p>
        </div>
      </div>

      <SalesInvoiceTable />
      <SalesInvoiceImportDialog />
      <SalesInvoiceDetailDialog />
      <DeleteSalesInvoiceDialog />
    </div>
  )
}
