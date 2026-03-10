'use client'

import { DivisionTable } from '../table/division-table'
import { DivisionDialogs } from '../dialogs/division-dialogs'

export function DivisionListPage() {
  return (
    <div className="flex flex-col gap-6 p-6">
      <div className="flex items-center justify-between rounded-lg bg-muted/90 p-10">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Division Management</h1>
          <p className="text-muted-foreground">Manage your division records</p>
        </div>
      </div>

      <DivisionTable />
      <DivisionDialogs />
    </div>
  )
}
