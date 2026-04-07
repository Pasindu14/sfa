'use client'

import { Clock } from 'lucide-react'
import { Card, CardContent } from '@/components/ui/card'
import { RouteCancellationTable } from '../table/route-cancellation-table'
import { RouteCancellationDialogs } from '../dialogs/route-cancellation-dialogs'
import { useRouteCancellationDataTable } from '../../hooks/route-cancellation.hooks'

function PendingCountCard() {
  const { data, isLoading } = useRouteCancellationDataTable(1, 1, '')

  const count = isLoading ? null : (data?.pagination.total_items ?? 0)

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
            <p className="text-xs text-muted-foreground">Pending cancellation requests</p>
          </div>
        </div>
      </CardContent>
    </Card>
  )
}

export function RouteCancellationListPage() {
  return (
    <div className="flex flex-col gap-6 p-6">
      {/* Header */}
      <div className="flex items-center justify-between bg-muted/90 p-10 rounded-lg">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Route Cancellation Requests</h1>
          <p className="text-muted-foreground">
            Review and action route assignment cancellation requests submitted by supervisors.
          </p>
        </div>
      </div>

      {/* KPI */}
      <PendingCountCard />

      {/* Table */}
      <RouteCancellationTable />
      <RouteCancellationDialogs />
    </div>
  )
}
