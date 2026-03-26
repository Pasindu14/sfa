'use client'

import { GeoAssignmentFilterCard } from '../filters/geo-assignment-filter-card'
import { UserGeoAssignmentTable } from '../table/user-geo-assignment-table'
import { UserGeoAssignmentDialogs } from '../dialogs/user-geo-assignment-dialogs'

export function UserGeoAssignmentListPage() {
  return (
    <div className="flex flex-col gap-6 p-6">
      <div className="flex items-center justify-between bg-muted/90 p-10 rounded-lg">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Geo Assignments</h1>
          <p className="text-muted-foreground">Assign users to geographic coverage areas</p>
        </div>
      </div>

      <GeoAssignmentFilterCard />
      <UserGeoAssignmentTable />
      <UserGeoAssignmentDialogs />
    </div>
  )
}
