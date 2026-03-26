'use client'

import { UserReportingLineTable } from '../table/user-reporting-line-table'
import { UserReportingLineDialogs } from '../dialogs/user-reporting-line-dialogs'

export function UserReportingLineListPage() {
  return (
    <div className="flex flex-col gap-6 p-6">
      <div className="flex items-center justify-between bg-muted/90 p-10 rounded-lg">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">User Assignments</h1>
          <p className="text-muted-foreground">
            Manage reporting lines for the sales org chart
          </p>
        </div>
      </div>

      <UserReportingLineTable />
      <UserReportingLineDialogs />
    </div>
  )
}
