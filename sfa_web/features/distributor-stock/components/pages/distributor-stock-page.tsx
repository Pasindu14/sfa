'use client'

import { DistributorStockTable } from '../table/distributor-stock-table'

export function DistributorStockPage() {
  return (
    <div className="flex flex-col gap-6 p-6">
      <div className="flex items-center justify-between bg-muted/90 p-10 rounded-lg">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Stock Balance</h1>
          <p className="text-muted-foreground">Current stock levels for your inventory</p>
        </div>
      </div>
      <div className="[&_td:not(:last-child)]:border-r [&_th:not(:last-child)]:border-r">
        <DistributorStockTable />
      </div>
    </div>
  )
}
