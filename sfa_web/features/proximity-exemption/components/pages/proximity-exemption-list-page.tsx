'use client'

import { ShieldOff } from 'lucide-react'
import { ProximityExemptionTable } from '../table/proximity-exemption-table'
import { ProximityExemptionDialogs } from '../dialogs/proximity-exemption-dialogs'
import { useProximityExemptionDataTable } from '../../hooks/proximity-exemption.hooks'

function ExemptCount() {
  // Page size 1 — only the pagination total is wanted, not the rows. This shares
  // a query key with nothing else, so it stays a cheap standalone count.
  const { data, isLoading } = useProximityExemptionDataTable(1, 1, '')

  const count = isLoading ? null : (data?.pagination.total_items ?? 0)

  return (
    <div className="flex shrink-0 items-start gap-4 rounded-lg border bg-background p-5 shadow-sm">
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
  )
}

export function ProximityExemptionListPage() {
  return (
    <div className="flex flex-col gap-6 p-6">
      {/* Header — title left, live count right */}
      <div className="bg-muted/90 flex flex-wrap items-center justify-between gap-6 rounded-lg p-10">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Proximity Exemptions</h1>
          <p className="text-muted-foreground">
            Sales reps currently allowed to bill outlets outside the geofence.
          </p>
        </div>
        <ExemptCount />
      </div>

      {/* Table — "Grant Exemption" lives in its toolbar, beside Export */}
      <ProximityExemptionTable />
      <ProximityExemptionDialogs />
    </div>
  )
}
