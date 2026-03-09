'use client'

import { RegionTable } from '../table/region-table'
import { RegionDialogs } from '../dialogs/region-dialogs'

export function RegionListPage() {
  return (
    <div className="flex flex-col gap-6 p-6">
      <div className="flex items-center justify-between bg-muted/90 p-10 rounded-lg">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Region Management</h1>
          <p className="text-muted-foreground">Manage your region records</p>
        </div>
      </div>

      <RegionTable />
      <RegionDialogs />
    </div>
  )
}
