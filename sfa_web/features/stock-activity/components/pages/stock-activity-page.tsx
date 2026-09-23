'use client'

import { StockActivityTable } from '../table/stock-activity-table'

export function StockActivityPage() {
  return (
    <div className="flex flex-col gap-6 p-6">
      <div className="flex items-center justify-between bg-muted/90 p-10 rounded-lg">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Stock Activity Log</h1>
          <p className="text-muted-foreground">
            Every stock movement — who did what, when, and the balance before and after
          </p>
        </div>
      </div>
      <StockActivityTable />
    </div>
  )
}
