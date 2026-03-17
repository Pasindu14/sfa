'use client'

import { SalesOrderTable } from '../table/sales-order-table'
import { SalesOrderDialogs } from '../dialogs/sales-order-dialogs'

export function SalesOrderListPage() {
  return (
    <div className="flex flex-col gap-6 p-6">
      <div className="flex items-center justify-between bg-muted/90 p-10 rounded-lg">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Sales Orders</h1>
          <p className="text-muted-foreground">Manage and track sales order workflow</p>
        </div>
      </div>

      <SalesOrderTable />
      <SalesOrderDialogs />
    </div>
  )
}
