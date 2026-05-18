'use client'

import { DistributorGrnTable } from '../table/distributor-grn-table'

export function DistributorGrnPage() {
  return (
    <div className="flex flex-col gap-6 p-6">
      <div className="flex items-center justify-between bg-muted/90 p-10 rounded-lg">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Goods Received Notes</h1>
          <p className="text-muted-foreground">Track and confirm goods received from your sales invoices</p>
        </div>
      </div>
      <div className="[&_td:not(:last-child)]:border-r [&_th:not(:last-child)]:border-r">
        <DistributorGrnTable />
      </div>
    </div>
  )
}
