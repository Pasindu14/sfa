'use client'

import { StockTable } from '../table/stock-table'

export function StockPage() {
  return (
    <div className="flex flex-col gap-6 p-6">
      <div className="flex items-center justify-between bg-muted/90 p-10 rounded-lg">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Stock Management</h1>
          <p className="text-muted-foreground">View current stock levels per distributor</p>
        </div>
      </div>
      <StockTable />
    </div>
  )
}
