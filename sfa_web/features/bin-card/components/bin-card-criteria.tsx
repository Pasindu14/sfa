'use client'

import { Search, RotateCcw, FileDown, FileSpreadsheet, Loader2, Package } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { AsyncSelect } from '@/components/async-select'
import { useBinCardFilters } from '../store'
import { useBinCardIsFetching } from '../hooks/bin-card.hooks'
import { getDistributorsAction } from '@/features/distributor/actions/distributor.actions'
import type { DistributorDto } from '@/features/distributor/schema/distributor.schema'
import type { BinCardResponse } from '../schema/bin-card.schema'
import { exportBinCardExcel, exportBinCardPdf } from '../lib/bin-card-export'

async function fetchDistributors(search?: string): Promise<DistributorDto[]> {
  if (!search || search.trim().length === 0) return []
  const result = await getDistributorsAction(1, 50, search.trim())
  if (!result.success) return []
  return result.data.distributors
}

export function BinCardCriteria({ data }: { data?: BinCardResponse }) {
  const {
    distributorId,
    from,
    to,
    setDistributorId,
    setFrom,
    setTo,
    applyFilters,
    reset,
  } = useBinCardFilters()
  const isFetching = useBinCardIsFetching()

  const canSearch = !!distributorId && !!from && !!to && from <= to
  const hasData = !!data && data.rows.length > 0

  return (
    <div className="flex flex-wrap items-end gap-3 rounded-lg border bg-card px-4 py-3">
      {/* Distributor */}
      <div className="flex flex-col gap-1.5">
        <label className="flex items-center gap-1 text-xs font-medium text-muted-foreground">
          <Package className="h-3 w-3" />
          Distributor
        </label>
        <AsyncSelect<DistributorDto>
          label="Distributor"
          placeholder="Search distributor..."
          fetcher={fetchDistributors}
          value={distributorId?.toString() ?? ''}
          onChange={(val) => setDistributorId(val ? Number(val) : null)}
          getOptionValue={(d) => d.id.toString()}
          getDisplayValue={(d) => <span className="text-sm">{d.name}</span>}
          renderOption={(d) => (
            <div className="flex flex-col gap-0.5 py-0.5">
              <span className="text-sm font-medium">{d.name}</span>
              {d.phone && <span className="text-xs text-muted-foreground">{d.phone}</span>}
            </div>
          )}
          notFound={
            <div className="py-4 text-center text-sm text-muted-foreground">
              Type to search distributors…
            </div>
          }
          noResultsMessage="No distributors found"
          width="280px"
          triggerClassName="h-8"
          clearable
        />
      </div>

      {/* From */}
      <div className="flex flex-col gap-1.5">
        <label className="text-xs font-medium text-muted-foreground">From</label>
        <Input
          type="date"
          value={from}
          max={to || undefined}
          onChange={(e) => setFrom(e.target.value)}
          className="h-8 w-40 text-sm"
        />
      </div>

      {/* To */}
      <div className="flex flex-col gap-1.5">
        <label className="text-xs font-medium text-muted-foreground">To</label>
        <Input
          type="date"
          value={to}
          min={from || undefined}
          onChange={(e) => setTo(e.target.value)}
          className="h-8 w-40 text-sm"
        />
      </div>

      <div className="flex items-center gap-2">
        <Button onClick={applyFilters} disabled={!canSearch || isFetching} className="h-8 gap-2">
          {isFetching ? (
            <Loader2 className="h-3.5 w-3.5 animate-spin" />
          ) : (
            <Search className="h-3.5 w-3.5" />
          )}
          {isFetching ? 'Loading...' : 'Search'}
        </Button>

        <Button variant="ghost" size="sm" onClick={reset} className="h-8 gap-1.5 text-muted-foreground">
          <RotateCcw className="h-3.5 w-3.5" />
          Clear
        </Button>

        <Button
          variant="outline"
          size="sm"
          disabled={!hasData}
          onClick={() => data && exportBinCardPdf(data)}
          className="h-8 gap-1.5"
        >
          <FileDown className="h-3.5 w-3.5" />
          PDF
        </Button>

        <Button
          variant="outline"
          size="sm"
          disabled={!hasData}
          onClick={() => data && void exportBinCardExcel(data)}
          className="h-8 gap-1.5"
        >
          <FileSpreadsheet className="h-3.5 w-3.5" />
          Excel
        </Button>
      </div>
    </div>
  )
}
