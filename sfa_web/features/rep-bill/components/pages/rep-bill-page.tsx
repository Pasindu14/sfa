'use client'

import { RepBillTable } from '../table/rep-bill-table'
import { RepBillDetailDialog } from '../dialogs/rep-bill-detail-dialog'

export function RepBillPage() {
  return (
    <div className="flex flex-col gap-6 p-6">
      {/* Hero card carries the title only. Filters live in their own row below it — the date
          picker's popover trigger gets clipped by this card's padded edge. */}
      <div className="rounded-lg bg-muted/90 p-10">
        <h1 className="text-3xl font-bold tracking-tight">Rep Bills</h1>
        <p className="text-muted-foreground">
          Pick a date range, a supervisor, and one of their sales reps to see the bills they wrote
        </p>
      </div>

      <RepBillTable />
      <RepBillDetailDialog />
    </div>
  )
}
