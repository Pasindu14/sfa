'use client'

import { DailyRouteAssignmentTable } from '../table/daily-route-assignment-table'
import { DailyRouteAssignmentDialogs } from '../dialogs/daily-route-assignment-dialogs'

export function DailyRouteAssignmentListPage() {
  return (
    <div className="flex flex-col gap-6 p-6">
      <div className="flex items-center justify-between bg-muted/90 p-10 rounded-lg">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Route Assignments</h1>
          <p className="text-muted-foreground">
            Assign daily routes to sales reps
          </p>
        </div>
      </div>

      <DailyRouteAssignmentTable />
      <DailyRouteAssignmentDialogs />
    </div>
  )
}
