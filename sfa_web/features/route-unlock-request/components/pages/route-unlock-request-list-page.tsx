'use client'

import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { AllRouteUnlockTable, PendingRouteUnlockTable } from '../table/route-unlock-request-table'
import { RouteUnlockRequestDialogs } from '../dialogs/route-unlock-request-dialogs'
import { RouteUnlockRequestDetailSheet } from '../sheets/route-unlock-request-detail-sheet'

export function RouteUnlockRequestListPage() {
  return (
    <div className="flex flex-col gap-6 p-6">
      {/* Header */}
      <div className="flex items-center justify-between bg-muted/90 p-10 rounded-lg">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Route Unlock Requests</h1>
          <p className="text-muted-foreground">
            Reps asking to reach every outlet on today&apos;s route. Supervisors review on mobile;
            admins can act here when the supervisor is unavailable.
          </p>
        </div>
      </div>

      <Tabs defaultValue="pending">
        <TabsList>
          <TabsTrigger value="pending">Pending</TabsTrigger>
          <TabsTrigger value="all">All</TabsTrigger>
        </TabsList>

        <TabsContent value="pending" className="mt-4">
          <PendingRouteUnlockTable />
        </TabsContent>

        <TabsContent value="all" className="mt-4">
          <AllRouteUnlockTable />
        </TabsContent>
      </Tabs>

      <RouteUnlockRequestDetailSheet />
      <RouteUnlockRequestDialogs />
    </div>
  )
}
