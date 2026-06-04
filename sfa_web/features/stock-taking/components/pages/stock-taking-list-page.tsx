'use client'

import { StockTakingTable } from '../table/stock-taking-table'
import { StockTakingDialogs } from '../dialogs/stock-taking-dialogs'

export function StockTakingListPage() {
  return (
    <div className="flex flex-col gap-6 p-6">
      <div className="flex items-center justify-between bg-muted/90 p-10 rounded-lg">
        <div className="flex items-center gap-4">
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
