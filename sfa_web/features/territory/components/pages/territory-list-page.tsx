'use client'

import { TerritoryTable } from '../table/territory-table'
import { TerritoryDialogs } from '../dialogs/territory-dialogs'

export function TerritoryListPage() {
  return (
    <div className="flex flex-col gap-6 p-6">
      <div className="flex items-center justify-between rounded-lg bg-muted/90 p-10">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Territory Management</h1>
          <p className="text-muted-foreground">Manage your territory records</p>
        </div>
      </div>

      <TerritoryTable />
      <TerritoryDialogs />
    </div>
  )
}
