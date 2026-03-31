'use client'

import { GrnTable } from '../table/grn-table'

export function GrnPage() {
  return (
    <div className="flex flex-col gap-6 p-6">
      <div className="flex items-center justify-between bg-muted/90 p-10 rounded-lg">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Goods Received Notes</h1>
          <p className="text-muted-foreground">
            Track and confirm delivery of sales invoices
          </p>
        </div>
      </div>

      <GrnTable />
    </div>
  )
}
