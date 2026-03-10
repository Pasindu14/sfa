'use client'

import { RouteTable } from '../table/route-table'
import { RouteDialogs } from '../dialogs/route-dialogs'

export function RouteListPage() {
  return (
    <div className="flex flex-col gap-6 p-6">
      <div className="flex items-center justify-between rounded-lg bg-muted/90 p-10">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Route Management</h1>
          <p className="text-muted-foreground">Manage your route records</p>
        </div>
      </div>

      <RouteTable />
      <RouteDialogs />
    </div>
  )
}
