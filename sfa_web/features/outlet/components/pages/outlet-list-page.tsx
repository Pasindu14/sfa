'use client'

import { OutletTable } from '../table/outlet-table'
import { OutletDialogs } from '../dialogs/outlet-dialogs'

export function OutletListPage() {
  return (
    <div className="flex flex-col gap-6 p-6">
      <div className="flex items-center justify-between bg-muted/90 p-10 rounded-lg">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Outlet Management</h1>
          <p className="text-muted-foreground">Manage your outlet network and customer locations</p>
        </div>
      </div>

      <OutletTable />
      <OutletDialogs />
    </div>
  )
}
