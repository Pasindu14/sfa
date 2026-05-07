'use client'

import { useCallback, useEffect } from 'react'
import { Search, RotateCcw, Package, Loader2, Layers } from 'lucide-react'
import { DataTable } from '@/components/data-table/data-table'
import { Button } from '@/components/ui/button'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { AsyncSelect } from '@/components/async-select'
import { useStockFilters } from '../../store'
import { useStockDataTable, useStockIsFetching } from '../../hooks/stock.hooks'
import { getStockColumns } from '../columns/stock-columns'
import { getDistributorsAction } from '@/features/distributor/actions/distributor.actions'
import type { DistributorDto } from '@/features/distributor/schema/distributor.schema'
import type { StockTypeFilter } from '../../store/stock.filter-store'

// ── Distributor fetcher ───────────────────────────────────────────────────

async function fetchDistributors(search?: string): Promise<DistributorDto[]> {
  if (!search || search.trim().length === 0) return []
  const result = await getDistributorsAction(1, 50, search.trim())
  if (!result.success) return []
  return result.data.distributors
}

// ── Filter form ───────────────────────────────────────────────────────────

function StockFilterForm({
  distributorId,
  stockType,
  hasLoaded,
  isLoading,
  onDistributorChange,
  onStockTypeChange,
  onLoad,
  onReset,
}: {
  distributorId: number | null
  stockType: StockTypeFilter
  hasLoaded: boolean
  isLoading: boolean
  onDistributorChange: (id: number | null) => void
  onStockTypeChange: (type: StockTypeFilter) => void
  onLoad: () => void
  onReset: () => void
}) {
  return (
    <div className="flex flex-wrap items-end gap-3 rounded-lg border bg-card px-4 py-3">
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
          onChange={(val) => onDistributorChange(val ? Number(val) : null)}
          getOptionValue={(d) => d.id.toString()}
          getDisplayValue={(d) => <span className="text-sm">{d.name}</span>}
          renderOption={(d) => (
            <div className="flex flex-col gap-0.5 py-0.5">
              <span className="text-sm font-medium">{d.name}</span>
              {d.phone && (
                <span className="text-xs text-muted-foreground">{d.phone}</span>
              )}
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

      <div className="flex flex-col gap-1.5">
        <label className="flex items-center gap-1 text-xs font-medium text-muted-foreground">
          <Layers className="h-3 w-3" />
          Stock Type
        </label>
        <Select
          value={stockType ?? 'all'}
          onValueChange={(v) => onStockTypeChange(v === 'all' ? null : (v as StockTypeFilter))}
        >
          <SelectTrigger className="h-8 w-36 text-sm">
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">All Types</SelectItem>
            <SelectItem value="Normal">Normal</SelectItem>
            <SelectItem value="FreeIssue">Free Issue</SelectItem>
          </SelectContent>
        </Select>
      </div>

      <div className="flex items-center gap-2">
        <Button
          onClick={onLoad}
          disabled={isLoading || !distributorId}
          className="h-8 gap-2"
        >
          {isLoading
            ? <Loader2 className="h-3.5 w-3.5 animate-spin" />
            : <Search className="h-3.5 w-3.5" />}
          {isLoading ? 'Loading...' : hasLoaded ? 'Reload' : 'Load Data'}
        </Button>
        {hasLoaded && (
          <Button
            variant="ghost"
            size="sm"
            onClick={onReset}
            className="h-8 gap-1.5 text-muted-foreground"
          >
            <RotateCcw className="h-3.5 w-3.5" />
            Reset
          </Button>
        )}
      </div>
    </div>
  )
}

// ── Table ─────────────────────────────────────────────────────────────────

export function StockTable() {
  const {
    distributorId,
    stockType,
    appliedFilters,
    setDistributorId,
    setStockType,
    applyFilters,
    reset,
  } = useStockFilters()
  const isFetching = useStockIsFetching()

  useEffect(() => {
    reset()
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  const getColumns = useCallback(() => getStockColumns(), [])

  return (
    <div className="flex flex-col gap-4">
      <StockFilterForm
        distributorId={distributorId}
        stockType={stockType}
        hasLoaded={!!appliedFilters}
        isLoading={isFetching}
        onDistributorChange={setDistributorId}
        onStockTypeChange={setStockType}
        onLoad={applyFilters}
        onReset={reset}
      />

      {appliedFilters ? (
        <DataTable
          key={`${appliedFilters.distributorId}-${appliedFilters.stockType ?? 'all'}-${appliedFilters.loadCount}`}
          config={{
            enableRowSelection: false,
            enableSearch: true,
            enableDateFilter: false,
            enableExport: false,
            enableColumnResizing: false,
            enableUrlState: false,
            columnResizingTableId: 'stock-table',
            searchPlaceholder: 'Search by code or description...',
          }}
          getColumns={getColumns}
          fetchDataFn={useStockDataTable}
          defaultPageSize={50}
          exportConfig={{
            entityName: 'stock',
            columnMapping: {
              productCode: 'Product Code',
              productDescription: 'Description',
              quantityOnHand: 'Qty on Hand',
              lastUpdatedAt: 'Last Updated',
            },
            columnWidths: [{ wch: 15 }, { wch: 35 }, { wch: 12 }, { wch: 20 }],
            headers: ['Product Code', 'Description', 'Qty on Hand', 'Last Updated'],
          }}
          idField="id"
        />
      ) : (
        <div className="flex flex-col items-center justify-center gap-2 rounded-lg border border-dashed py-16 text-center">
          <Package className="h-8 w-8 text-muted-foreground/40" />
          <p className="text-sm font-medium text-muted-foreground">
            Select a distributor and click{' '}
            <span className="font-semibold">Load Data</span> to view stock levels
          </p>
          <p className="text-xs text-muted-foreground/60">
            Search by product code or description after loading
          </p>
        </div>
      )}
    </div>
  )
}
