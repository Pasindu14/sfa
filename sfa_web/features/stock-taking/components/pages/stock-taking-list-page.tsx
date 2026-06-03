'use client'

import { ClipboardList } from 'lucide-react'
import { StockTakingTable } from '../table/stock-taking-table'
import { StockTakingDialogs } from '../dialogs/stock-taking-dialogs'

export function StockTakingListPage() {
  return (
    <div className="flex flex-col gap-6 p-6">
      <div className="flex items-center justify-between bg-muted/90 p-10 rounded-lg">
        <div className="flex items-center gap-4">
          <div className="flex h-14 w-14 items-center justify-center rounded-xl bg-primary/10 text-primary">
            <ClipboardList className="h-7 w-7" />
          </div>
          <div>
            <h1 className="text-3xl font-bold tracking-tight">Stock Taking</h1>
            <p className="text-muted-foreground">Manage counting periods and reconcile distributor inventory</p>
          </div>
        </div>
      </div>
      <StockTakingTable />
      <StockTakingDialogs />
    </div>
  )
}
