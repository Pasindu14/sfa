'use client'

import { useCallback, useEffect, useState } from 'react'
import { Search, RotateCcw, Package, Loader2, Layers, FileSpreadsheet } from 'lucide-react'
import { toast } from 'sonner'
import { DataTable } from '@/components/data-table/data-table'
import { Button } from '@/components/ui/button'
import { Switch } from '@/components/ui/switch'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { AsyncSelect } from '@/components/async-select'
import { useStockFilters } from '../../store'
import { useStockDataTable, useStockIsFetching, useStockValueSummary } from '../../hooks/stock.hooks'
import { formatMoney, getStockColumns } from '../columns/stock-columns'
import { exportStockListExcel } from '../../lib/stock-export'
import { fetchActiveDistributorsForSelect } from '@/features/distributor/actions/distributor.actions'
import type { DistributorDto } from '@/features/distributor/schema/distributor.schema'
import type { StockTypeFilter } from '../../store/stock.filter-store'

// ── Distributor fetcher ───────────────────────────────────────────────────


// ── Filter form ───────────────────────────────────────────────────────────

function StockFilterForm({
  distributorId,
  stockType,
  includeZeroStock,
  hasLoaded,
  isLoading,
  onDistributorChange,
  onStockTypeChange,
  onIncludeZeroStockChange,
  onLoad,
  onReset,
}: {
  distributorId: number | null
  stockType: StockTypeFilter
  includeZeroStock: boolean
  hasLoaded: boolean
  isLoading: boolean
  onDistributorChange: (id: number | null) => void
  onStockTypeChange: (type: StockTypeFilter) => void
  onIncludeZeroStockChange: (include: boolean) => void
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
          fetcher={fetchActiveDistributorsForSelect}
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

      <label className="flex h-8 cursor-pointer items-center gap-2 text-sm">
        <Switch checked={includeZeroStock} onCheckedChange={onIncludeZeroStockChange} />
        Show zero stock
      </label>

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

// ── Stock value summary ───────────────────────────────────────────────────

function StockValueSummary() {
  const { items, totalValue, unpricedCount, itemCount, isLoading, hasData, appliedFilters } = useStockValueSummary()
  const [exporting, setExporting] = useState(false)
  if (isLoading || !hasData) return null

  // Exports every item for the loaded filters — the whole list, not the table's current page.
  const handleExport = async () => {
    if (items.length === 0) return
    setExporting(true)
    try {
      const typeLabel = appliedFilters?.stockType === 'FreeIssue' ? 'Free Issue'
        : appliedFilters?.stockType === 'Normal' ? 'Normal' : 'All types'
      const zeroLabel = appliedFilters?.includeZeroStock ? 'incl. zero stock' : 'held items only'
      await exportStockListExcel(items[0].distributorName, items, `${typeLabel}, ${zeroLabel}`)
    } catch {
      toast.error('Failed to export stock list')
    } finally {
      setExporting(false)
    }
  }

  return (
    <div className="flex flex-wrap items-center gap-x-6 gap-y-1 rounded-lg border bg-card px-4 py-3">
      <div>
        <span className="block text-xs text-muted-foreground">Total stock value (LKR)</span>
        <span className="text-lg font-semibold tabular-nums">{formatMoney(totalValue)}</span>
      </div>
      <div>
        <span className="block text-xs text-muted-foreground">Items</span>
        <span className="text-lg font-semibold tabular-nums">{itemCount}</span>
      </div>
      <p className="text-xs text-muted-foreground">
        Valued at the default pricing structure&apos;s dealer pack price.
        {unpricedCount > 0 && (
          <span className="text-amber-600">
            {' '}{unpricedCount} item{unpricedCount === 1 ? '' : 's'} with stock have no default price and are not included.
          </span>
        )}
      </p>
      <Button
        variant="outline"
        size="sm"
        className="ml-auto h-8 gap-1.5"
        onClick={handleExport}
        disabled={exporting || items.length === 0}
      >
        {exporting ? <Loader2 className="h-3.5 w-3.5 animate-spin" /> : <FileSpreadsheet className="h-3.5 w-3.5" />}
        {exporting ? 'Exporting...' : 'Export Excel'}
      </Button>
    </div>
  )
}

// ── Table ─────────────────────────────────────────────────────────────────

export function StockTable() {
  const {
    distributorId,
    stockType,
    includeZeroStock,
    appliedFilters,
    setDistributorId,
    setStockType,
    setIncludeZeroStock,
    applyFilters,
    reset,
  } = useStockFilters()
  const isFetching = useStockIsFetching()

  useEffect(() => {
    reset()
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  const getColumns = useCallback(() => getStockColumns({ showValue: true }), [])

  return (
    <div className="flex flex-col gap-4">
      <StockFilterForm
        distributorId={distributorId}
        stockType={stockType}
        includeZeroStock={includeZeroStock}
        hasLoaded={!!appliedFilters}
        isLoading={isFetching}
        onDistributorChange={setDistributorId}
        onStockTypeChange={setStockType}
        onIncludeZeroStockChange={setIncludeZeroStock}
        onLoad={applyFilters}
        onReset={reset}
      />

      {appliedFilters && <StockValueSummary />}

      {appliedFilters ? (
        <DataTable
          key={`${appliedFilters.distributorId}-${appliedFilters.stockType ?? 'all'}-${appliedFilters.includeZeroStock}-${appliedFilters.loadCount}`}
          config={{
            enableRowSelection: false,
            enableSearch: true,
            enableDateFilter: false,
            enableExport: false,
            enableColumnResizing: true,
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
