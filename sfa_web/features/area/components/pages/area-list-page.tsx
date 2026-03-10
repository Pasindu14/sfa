'use client'

import { AreaTable } from '../table/area-table'
import { AreaDialogs } from '../dialogs/area-dialogs'

export function AreaListPage() {
  return (
    <div className="flex flex-col gap-6 p-6">
      <div className="flex items-center justify-between rounded-lg bg-muted/90 p-10">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Area Management</h1>
          <p className="text-muted-foreground">Manage your area records</p>
        </div>
      </div>

      <AreaTable />
      <AreaDialogs />
    </div>
  )
}
