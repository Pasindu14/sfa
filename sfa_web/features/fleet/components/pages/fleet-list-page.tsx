'use client'

import { FleetTable } from '../table/fleet-table'
import { FleetDialogs } from '../dialogs/fleet-dialogs'

export function FleetListPage() {
  return (
    <div className="flex flex-col gap-6 p-6">
      <div className="flex items-center justify-between bg-muted/90 p-10 rounded-lg">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Fleet Management</h1>
          <p className="text-muted-foreground">Manage fleet master records</p>
        </div>
      </div>

      <FleetTable />
      <FleetDialogs />
    </div>
  )
}
