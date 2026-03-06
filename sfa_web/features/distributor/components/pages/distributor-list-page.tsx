'use client'

import { DistributorTable } from '../table/distributor-table'
import { DistributorDialogs } from '../dialogs/distributor-dialogs'

export function DistributorListPage() {
  return (
    <div className="flex flex-col gap-6 p-6">
      <div className="flex items-center justify-between bg-muted/90 p-10 rounded-lg">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Distributor Management</h1>
          <p className="text-muted-foreground">Manage your distributor network and partnerships</p>
        </div>
      </div>

      <DistributorTable />
      <DistributorDialogs />
    </div>
  )
}
