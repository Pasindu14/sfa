'use client'

import { useCallback, useMemo, useRef, useState } from 'react'
import { ArrowRightLeft, Download, History, Loader2, PackageX, RotateCcw, Store } from 'lucide-react'
import { toast } from 'sonner'
import { AsyncSelect } from '@/components/async-select'
import { Button } from '@/components/ui/button'
import {
  Card,
  CardAction,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from '@/components/ui/card'
import { Label } from '@/components/ui/label'
import { Skeleton } from '@/components/ui/skeleton'
import { Textarea } from '@/components/ui/textarea'
import {
  fetchActiveDistributorsForSelect,
  fetchInactiveDistributorsForSelect,
} from '@/features/distributor/actions/distributor.actions'
import type { DistributorDto } from '@/features/distributor/schema/distributor.schema'
import { useDistributorStock } from '@/features/stock/hooks/stock.hooks'
import { useCreateStockTransfer } from '../../hooks/stock-transfer.hooks'
import { stockLineKey } from '@/features/stock/lib/quantity'
import { exportDistributorStockExcel } from '../../lib/stock-transfer-export'
import { StockTransferConfirmDialog, type ConfirmLine } from '../dialogs/stock-transfer-confirm-dialog'
import { StockTransferHistoryTable } from '../table/stock-transfer-history-table'
import { TransferLinesTable } from '../table/transfer-lines-table'

const NOTES_MAX = 500

function DistributorOption({ d }: { d: DistributorDto }) {
  return (
    <div className="flex flex-col gap-0.5 py-0.5">
      <span className="text-sm font-medium">{d.name}</span>
      {d.phone && <span className="text-xs text-muted-foreground">{d.phone}</span>}
    </div>
  )
}

export function StockTransferPage() {
  // Bumped after a successful transfer to remount the pickers with a clean selection.
  const [formKey, setFormKey] = useState(0)
  const [source, setSource] = useState<{ id: number; name: string } | null>(null)
  const [target, setTarget] = useState<{ id: number; name: string } | null>(null)
  const [notes, setNotes] = useState('')
  // Per-line transfer qty in pieces. Lines without an override default to the full balance.
  const [overrides, setOverrides] = useState<Record<string, number>>({})
  const [confirmOpen, setConfirmOpen] = useState(false)
  // One key per confirmed submission, so a retry of the same transfer is deduplicated by the API.
  const [idempotencyKey, setIdempotencyKey] = useState('')
  const [exporting, setExporting] = useState(false)

  // Last options each picker loaded — used to resolve the selected distributor's name.
  const sourceOptions = useRef<DistributorDto[]>([])
  const targetOptions = useRef<DistributorDto[]>([])

  const sourceId = source?.id ?? null
  const { data: stock, isLoading: stockLoading, isError: stockError } = useDistributorStock(sourceId)

  const items = useMemo(
    () => (stock ?? []).filter((i) => i.quantityOnHand > 0),
    [stock],
  )

  const quantities = useMemo(() => {
    const map: Record<string, number> = {}
    for (const item of items) {
      const key = stockLineKey(item.productId, item.stockType)
      map[key] = Math.min(Math.max(overrides[key] ?? item.quantityOnHand, 0), item.quantityOnHand)
    }
    return map
  }, [items, overrides])

  const confirmLines: ConfirmLine[] = useMemo(
    () =>
      items
        .map((item) => ({ item, quantity: quantities[stockLineKey(item.productId, item.stockType)] ?? 0 }))
        .filter((l) => l.quantity > 0),
    [items, quantities],
  )

  const fetchSources = useCallback(async (q?: string) => {
    const list = await fetchInactiveDistributorsForSelect(q)
    sourceOptions.current = list
    return list
  }, [])

  const fetchTargets = useCallback(
    async (q?: string) => {
      const list = (await fetchActiveDistributorsForSelect(q)).filter((d) => d.id !== sourceId)
      targetOptions.current = list
      return list
    },
    [sourceId],
  )

  const resetForm = useCallback(() => {
    setSource(null)
    setTarget(null)
    setNotes('')
    setOverrides({})
    setConfirmOpen(false)
    setIdempotencyKey('')
    setFormKey((k) => k + 1)
  }, [])

  const createMutation = useCreateStockTransfer(resetForm)

  const handleSourceChange = (val: string) => {
    const id = val ? Number(val) : null
    const match = id ? sourceOptions.current.find((d) => d.id === id) : undefined
    setSource(id ? { id, name: match?.name ?? `Distributor #${id}` } : null)
    setOverrides({})
    if (id && target?.id === id) setTarget(null)
  }

  const handleTargetChange = (val: string) => {
    const id = val ? Number(val) : null
    const match = id ? targetOptions.current.find((d) => d.id === id) : undefined
    setTarget(id ? { id, name: match?.name ?? `Distributor #${id}` } : null)
  }

  const handleExport = async () => {
    if (!source || items.length === 0) return
    setExporting(true)
    try {
      await exportDistributorStockExcel(source.name, items)
    } catch {
      toast.error('Failed to generate the Excel file')
    } finally {
      setExporting(false)
    }
  }

  const openConfirm = () => {
    setIdempotencyKey(crypto.randomUUID())
    setConfirmOpen(true)
  }

  const submit = () => {
    if (!source || !target || confirmLines.length === 0) return
    createMutation.mutate({
      idempotencyKey,
      data: {
        sourceDistributorId: source.id,
        targetDistributorId: target.id,
        notes: notes.trim() || undefined,
        lines: confirmLines.map(({ item, quantity }) => ({
          productId: item.productId,
          stockType: item.stockType,
          quantity,
        })),
      },
    }, {
      // The API rejected this submission outright — close so a retry reopens with a fresh key.
      onError: () => setConfirmOpen(false),
    })
  }

  const isPending = createMutation.isPending
  const canTransfer = !!source && !!target && confirmLines.length > 0 && !isPending

  return (
    <div className="flex flex-col gap-6 p-6">
      <div className="flex items-center justify-between bg-muted/90 p-10 rounded-lg">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Stock Transfer</h1>
          <p className="text-muted-foreground">
            Move the remaining stock of a closed distributor to an active one
          </p>
        </div>
      </div>

      {/* ── 1. Source distributor + remaining stock ─────────────────────── */}
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <PackageX className="h-4 w-4" />
            Closed distributor
          </CardTitle>
          <CardDescription>
            Select a deactivated distributor to see its remaining stock. Each line defaults to the
            full balance — lower or zero it to transfer less.
          </CardDescription>
          {source && items.length > 0 && (
            <CardAction className="flex gap-2">
              <Button
                variant="ghost"
                size="sm"
                className="gap-1.5"
                onClick={() => setOverrides({})}
                disabled={isPending}
              >
                <RotateCcw className="h-3.5 w-3.5" />
                Full balance
              </Button>
              <Button variant="outline" size="sm" className="gap-1.5" onClick={handleExport} disabled={exporting}>
                {exporting ? <Loader2 className="h-3.5 w-3.5 animate-spin" /> : <Download className="h-3.5 w-3.5" />}
                Download Excel
              </Button>
            </CardAction>
          )}
        </CardHeader>
        <CardContent className="flex flex-col gap-4">
          <div className="flex flex-col gap-1.5">
            <Label className="text-xs text-muted-foreground">Source distributor</Label>
            <AsyncSelect<DistributorDto>
              key={`source-${formKey}`}
              label="Source distributor"
              placeholder="Search closed distributors..."
              fetcher={fetchSources}
              value={source?.id.toString() ?? ''}
              onChange={handleSourceChange}
              getOptionValue={(d) => d.id.toString()}
              getDisplayValue={(d) => <span className="text-sm">{d.name}</span>}
              renderOption={(d) => <DistributorOption d={d} />}
              notFound={
                <div className="py-4 text-center text-sm text-muted-foreground">
                  No closed distributors found
                </div>
              }
              noResultsMessage="No closed distributors found"
              width="320px"
              triggerClassName="h-9"
              disabled={isPending}
              clearable
            />
          </div>

          {!source ? (
            <div className="flex flex-col items-center justify-center gap-2 rounded-lg border border-dashed py-12 text-center">
              <PackageX className="h-8 w-8 text-muted-foreground/40" />
              <p className="text-sm text-muted-foreground">
                Select a closed distributor to load its remaining stock
              </p>
            </div>
          ) : stockLoading ? (
            <div className="flex flex-col gap-2">
              {Array.from({ length: 5 }).map((_, i) => (
                <Skeleton key={i} className="h-10 w-full" />
              ))}
            </div>
          ) : stockError ? (
            <p className="text-sm text-destructive">Could not load stock for this distributor.</p>
          ) : items.length === 0 ? (
            <div className="flex flex-col items-center justify-center gap-2 rounded-lg border border-dashed py-12 text-center">
              <p className="text-sm font-medium text-muted-foreground">
                {source.name} has no remaining stock to transfer
              </p>
            </div>
          ) : (
            <TransferLinesTable
              items={items}
              quantities={quantities}
              disabled={isPending}
              onQuantityChange={(key, pieces) => setOverrides((prev) => ({ ...prev, [key]: pieces }))}
            />
          )}
        </CardContent>
      </Card>

      {/* ── 2. Target distributor + submit ──────────────────────────────── */}
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Store className="h-4 w-4" />
            Transfer to
          </CardTitle>
          <CardDescription>Choose the active distributor that receives the stock.</CardDescription>
        </CardHeader>
        <CardContent className="flex flex-col gap-4">
          <div className="flex flex-col gap-1.5">
            <Label className="text-xs text-muted-foreground">Target distributor</Label>
            <AsyncSelect<DistributorDto>
              key={`target-${formKey}`}
              label="Target distributor"
              placeholder="Search active distributors..."
              fetcher={fetchTargets}
              value={target?.id.toString() ?? ''}
              onChange={handleTargetChange}
              getOptionValue={(d) => d.id.toString()}
              getDisplayValue={(d) => <span className="text-sm">{d.name}</span>}
              renderOption={(d) => <DistributorOption d={d} />}
              notFound={
                <div className="py-4 text-center text-sm text-muted-foreground">
                  Type to search distributors…
                </div>
              }
              noResultsMessage="No active distributors found"
              width="320px"
              triggerClassName="h-9"
              disabled={isPending}
              clearable
            />
          </div>

          <div className="flex flex-col gap-1.5">
            <Label htmlFor="stock-transfer-notes" className="text-xs text-muted-foreground">
              Notes (optional)
            </Label>
            <Textarea
              id="stock-transfer-notes"
              placeholder="Reason for the transfer, reference, etc."
              value={notes}
              maxLength={NOTES_MAX}
              onChange={(e) => setNotes(e.target.value)}
              disabled={isPending}
              className="max-w-2xl"
              rows={3}
            />
          </div>

          <div className="flex items-center gap-3">
            <Button onClick={openConfirm} disabled={!canTransfer} className="gap-2">
              <ArrowRightLeft className="h-4 w-4" />
              Transfer
            </Button>
            {source && items.length > 0 && confirmLines.length === 0 && (
              <span className="text-xs text-muted-foreground">Set a quantity on at least one line.</span>
            )}
          </div>
        </CardContent>
      </Card>

      {/* ── 3. History ──────────────────────────────────────────────────── */}
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <History className="h-4 w-4" />
            Transfer history
          </CardTitle>
        </CardHeader>
        <CardContent>
          <StockTransferHistoryTable />
        </CardContent>
      </Card>

      <StockTransferConfirmDialog
        open={confirmOpen}
        sourceName={source?.name ?? ''}
        targetName={target?.name ?? ''}
        notes={notes}
        lines={confirmLines}
        isPending={isPending}
        onOpenChange={setConfirmOpen}
        onConfirm={submit}
      />
    </div>
  )
}
