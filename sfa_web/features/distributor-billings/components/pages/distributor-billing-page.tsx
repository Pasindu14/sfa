'use client'

import { DistributorBillingTable } from '../table/distributor-billing-table'

export function DistributorBillingPage() {
  return (
    <div className="flex flex-col gap-6 p-6">
      <div className="flex items-center justify-between bg-muted/90 p-10 rounded-lg">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Bills</h1>
          <p className="text-muted-foreground">Sales transactions issued by your representatives</p>
        </div>
      </div>
      <div className="[&_td:not(:last-child)]:border-r [&_th:not(:last-child)]:border-r">
        <DistributorBillingTable />
      </div>
    </div>
  )
}
