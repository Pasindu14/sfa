'use client'

import { Clock } from 'lucide-react'
import { Card, CardContent } from '@/components/ui/card'
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { AllRouteUnlockTable, PendingRouteUnlockTable } from '../table/route-unlock-request-table'
import { RouteUnlockRequestDialogs } from '../dialogs/route-unlock-request-dialogs'
import { RouteUnlockRequestDetailSheet } from '../sheets/route-unlock-request-detail-sheet'
import { useRouteUnlockPendingCount } from '../../hooks/route-unlock-request.hooks'

function PendingCountCard() {
  const { data, isLoading, isError } = useRouteUnlockPendingCount()

  const count = isLoading || isError ? null : (data ?? 0)

  return (
    <Card className="border shadow-sm w-fit">
      <CardContent className="p-5">
        <div className="flex items-start gap-4">
          <div className="rounded-md p-2 bg-amber-50">
            <Clock className="h-4 w-4 text-amber-600" />
          </div>
          <div className="space-y-1">
            <p className="text-xs font-medium text-muted-foreground uppercase tracking-wide">
              Awaiting Review
            </p>
            <p className="text-3xl font-bold tracking-tight text-amber-600">
              {count === null ? '—' : count}
            </p>
            <p className="text-xs text-muted-foreground">Pending unlock requests today</p>
          </div>
        </div>
      </CardContent>
    </Card>
  )
}

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

      {/* KPI */}
      <PendingCountCard />

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
