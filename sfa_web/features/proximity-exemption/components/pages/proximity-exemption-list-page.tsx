'use client'

import { ShieldOff } from 'lucide-react'
import { Card, CardContent } from '@/components/ui/card'
import { ProximityExemptionTable } from '../table/proximity-exemption-table'
import { ProximityExemptionDialogs } from '../dialogs/proximity-exemption-dialogs'
import { useProximityExemptionDataTable } from '../../hooks/proximity-exemption.hooks'

function ExemptCountCard() {
  // Page size 1 — only the pagination total is wanted, not the rows.
  const { data, isLoading } = useProximityExemptionDataTable(1, 1, '')

  const count = isLoading ? null : (data?.pagination.total_items ?? 0)

  return (
    <Card className="w-fit border shadow-sm">
      <CardContent className="p-5">
        <div className="flex items-start gap-4">
          <div className="rounded-md bg-amber-50 p-2">
            <ShieldOff className="h-4 w-4 text-amber-600" />
          </div>
          <div className="space-y-1">
            <p className="text-muted-foreground text-xs font-medium tracking-wide uppercase">
              Currently Exempt
            </p>
            <p
              className={`text-3xl font-bold tracking-tight ${
                count ? 'text-amber-600' : 'text-foreground'
              }`}
            >
              {count === null ? '—' : count}
            </p>
            <p className="text-muted-foreground text-xs">
              {count === 1 ? 'Rep billing without' : 'Reps billing without'} a distance check
            </p>
          </div>
        </div>
      </CardContent>
    </Card>
  )
}

export function ProximityExemptionListPage() {
  return (
    <div className="flex flex-col gap-6 p-6">
      {/* Header */}
      <div className="bg-muted/90 flex items-center justify-between rounded-lg p-10">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Proximity Exemptions</h1>
          <p className="text-muted-foreground">
            Sales reps currently allowed to bill outlets outside the geofence. Grant a new
            exemption from a rep&apos;s row on the Users page.
          </p>
        </div>
      </div>

      {/* KPI */}
      <ExemptCountCard />

      {/* Table */}
      <ProximityExemptionTable />
      <ProximityExemptionDialogs />
    </div>
  )
}
