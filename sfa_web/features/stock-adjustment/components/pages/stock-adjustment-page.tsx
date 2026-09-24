'use client'

import { useCallback, useMemo, useRef, useState } from 'react'
import { ClipboardEdit, History, Plus, RotateCcw, Search, Store } from 'lucide-react'
import { toast } from 'sonner'
import { AsyncSelect } from '@/components/async-select'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import {
  Card,
  CardAction,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { Skeleton } from '@/components/ui/skeleton'
import { Textarea } from '@/components/ui/textarea'
import { fetchAllDistributorsForSelect } from '@/features/distributor/actions/distributor.actions'
import type { DistributorDto } from '@/features/distributor/schema/distributor.schema'
import { useActiveProductsFetcher } from '@/features/product/hooks/product.hooks'
import type { ProductLookupDto } from '@/features/product/schema/product.schema'
import { useDistributorStock } from '@/features/stock/hooks/stock.hooks'
import { stockLineKey } from '@/features/stock/lib/quantity'
import { useCreateStockAdjustment } from '../../hooks/stock-adjustment.hooks'
import {
  STOCK_ADJUSTMENT_REASONS,
  reasonLabel,
  type StockAdjustmentReason,
} from '../../schema/stock-adjustment.schema'
import { StockAdjustmentConfirmDialog, type ChangedLine } from '../dialogs/stock-adjustment-confirm-dialog'
import { StockAdjustmentHistoryTable } from '../table/stock-adjustment-history-table'
import { AdjustmentLinesTable, type AdjustmentRow } from '../table/adjustment-lines-table'

const NOTES_MAX = 500

type StockType = AdjustmentRow['stockType']

export function StockAdjustmentPage() {
  // Bumped after a successful adjustment to remount the pickers with a clean selection.
  const [formKey, setFormKey] = useState(0)
  const [distributor, setDistributor] = useState<{ id: number; name: string; isActive: boolean } | null>(null)
  // Rows added via "Add product" — appended after the distributor's existing stock lines.
  const [addedRows, setAddedRows] = useState<AdjustmentRow[]>([])
  // New balance per line in pieces. Lines without an entry keep their current balance.
  const [newQuantities, setNewQuantities] = useState<Record<string, number>>({})
  const [reason, setReason] = useState<StockAdjustmentReason | ''>('')
  const [notes, setNotes] = useState('')
  const [lineFilter, setLineFilter] = useState('')
  const [confirmOpen, setConfirmOpen] = useState(false)
  // One key per confirmed submission, so a retry of the same adjustment is deduplicated by the API.
  const [idempotencyKey, setIdempotencyKey] = useState('')

  // Add-product picker state; the key remounts the picker after each add.
  const [addKey, setAddKey] = useState(0)
  const [addProduct, setAddProduct] = useState<ProductLookupDto | null>(null)
  const [addStockType, setAddStockType] = useState<StockType>('Normal')

  // Last options each picker loaded — used to resolve the selected option from its value.
  const distributorOptions = useRef<DistributorDto[]>([])
  const productOptions = useRef<ProductLookupDto[]>([])

  const distributorId = distributor?.id ?? null
  const { data: stock, isLoading: stockLoading, isError: stockError } = useDistributorStock(distributorId)

  const rows: AdjustmentRow[] = useMemo(() => {
    const addedKeys = new Set(addedRows.map((r) => stockLineKey(r.productId, r.stockType)))
    const existing = (stock ?? [])
      // Only items with a balance; zero-stock products are brought in with "Add product".
      .filter((i) => i.quantityOnHand !== 0 && !addedKeys.has(stockLineKey(i.productId, i.stockType)))
      .map<AdjustmentRow>((i) => ({
        productId: i.productId,
        productCode: i.productCode,
        productDescription: i.productDescription,
        stockType: i.stockType,
        piecesPerPack: i.piecesPerPack,
        currentQuantity: i.quantityOnHand,
      }))
    // An added row takes its current balance from the live stock when a row exists there
    // (e.g. a zero-balance placeholder), so expectedQuantity always matches the server.
    const added = addedRows.map((r) => {
      const live = stock?.find((i) => i.productId === r.productId && i.stockType === r.stockType)
      return live
        ? { ...r, currentQuantity: live.quantityOnHand, piecesPerPack: live.piecesPerPack }
        : r
    })
    return [...existing, ...added]
  }, [stock, addedRows])

  const visibleRows = useMemo(() => {
    const q = lineFilter.trim().toLowerCase()
    if (!q) return rows
    return rows.filter(
      (r) => r.productCode.toLowerCase().includes(q) || r.productDescription.toLowerCase().includes(q),
    )
  }, [rows, lineFilter])

  const changedLines: ChangedLine[] = useMemo(
    () =>
      rows
        .map((row) => ({
          row,
          newQuantity: newQuantities[stockLineKey(row.productId, row.stockType)] ?? row.currentQuantity,
        }))
        .filter((l) => l.newQuantity !== l.row.currentQuantity),
    [rows, newQuantities],
  )

  const fetchDistributors = useCallback(async (q?: string) => {
    const list = await fetchAllDistributorsForSelect(q)
    distributorOptions.current = list
    return list
  }, [])

  const fetchActiveProducts = useActiveProductsFetcher()
  const fetchProducts = useCallback(
    async (q?: string) => {
      const list = await fetchActiveProducts(q)
      productOptions.current = list
      return list
    },
    [fetchActiveProducts],
  )

  const resetLines = () => {
    setAddedRows([])
    setNewQuantities({})
    setLineFilter('')
    setAddProduct(null)
    setAddKey((k) => k + 1)
  }

  const resetForm = useCallback(() => {
    setDistributor(null)
    setAddedRows([])
    setNewQuantities({})
    setReason('')
    setNotes('')
    setLineFilter('')
    setConfirmOpen(false)
    setIdempotencyKey('')
    setAddProduct(null)
    setAddStockType('Normal')
    setAddKey((k) => k + 1)
    setFormKey((k) => k + 1)
  }, [])

  const createMutation = useCreateStockAdjustment(resetForm)

  const handleDistributorChange = (val: string) => {
    const id = val ? Number(val) : null
    const match = id ? distributorOptions.current.find((d) => d.id === id) : undefined
    setDistributor(
      id ? { id, name: match?.name ?? `Distributor #${id}`, isActive: match?.isActive ?? true } : null,
    )
    resetLines()
  }

  const handleProductChange = (val: string) => {
    const id = val ? Number(val) : null
    setAddProduct(id ? productOptions.current.find((p) => p.id === id) ?? null : null)
  }

  const addKeyValue = addProduct ? stockLineKey(addProduct.id, addStockType) : null
  const isDuplicate = !!addKeyValue && rows.some((r) => stockLineKey(r.productId, r.stockType) === addKeyValue)

  const handleAddProduct = () => {
    if (!addProduct) return
    if (isDuplicate) {
      toast.error(`${addProduct.code} (${addStockType === 'FreeIssue' ? 'Free Issue' : 'Normal'}) is already listed`)
      return
    }
    setAddedRows((prev) => [
      ...prev,
      {
        productId: addProduct.id,
        productCode: addProduct.code,
        productDescription: addProduct.itemDescription,
        stockType: addStockType,
        // Product lookups carry no pack size; the live stock row (if any) fills it in.
        piecesPerPack: 0,
        currentQuantity: 0,
        added: true,
      },
    ])
    setAddProduct(null)
    setAddKey((k) => k + 1)
  }

  const handleRemove = (key: string) => {
    setAddedRows((prev) => prev.filter((r) => stockLineKey(r.productId, r.stockType) !== key))
    setNewQuantities((prev) => {
      const next = { ...prev }
      delete next[key]
      return next
    })
  }

  const openConfirm = () => {
    setIdempotencyKey(crypto.randomUUID())
    setConfirmOpen(true)
  }

  const submit = () => {
    if (!distributor || !reason || changedLines.length === 0) return
    createMutation.mutate({
      idempotencyKey,
      data: {
        distributorId: distributor.id,
        reason,
        notes: notes.trim() || undefined,
        lines: changedLines.map(({ row, newQuantity }) => ({
          productId: row.productId,
          stockType: row.stockType,
          expectedQuantity: row.currentQuantity,
          newQuantity,
        })),
      },
    }, {
      // The API rejected this submission outright — close so a retry reopens with a fresh key.
      onError: () => setConfirmOpen(false),
    })
  }

  const isPending = createMutation.isPending
  const notesRequired = reason === 'Other'
  const notesMissing = notesRequired && !notes.trim()
  const canSubmit = !!distributor && !!reason && !notesMissing && changedLines.length > 0 && !isPending

  const submitHint = !distributor
    ? null
    : changedLines.length === 0
      ? 'Change at least one balance.'
      : !reason
        ? 'Select a reason.'
        : notesMissing
          ? 'Notes are required when the reason is Other.'
          : null

  return (
    <div className="flex flex-col gap-6 p-6">
      <div className="flex items-center justify-between bg-muted/90 p-10 rounded-lg">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Stock Adjustment</h1>
          <p className="text-muted-foreground">
            Correct a distributor&apos;s stock balances with an audited adjustment
          </p>
        </div>
      </div>

      {/* ── 1. Distributor + stock lines ────────────────────────────────── */}
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Store className="h-4 w-4" />
            Distributor stock
          </CardTitle>
          <CardDescription>
            Set the new balance on each line to correct. Changed lines are highlighted.
          </CardDescription>
          {distributor && changedLines.length > 0 && (
            <CardAction>
              <Button
                variant="ghost"
                size="sm"
                className="gap-1.5"
                onClick={() => setNewQuantities({})}
                disabled={isPending}
              >
                <RotateCcw className="h-3.5 w-3.5" />
                Undo changes
              </Button>
            </CardAction>
          )}
        </CardHeader>
        <CardContent className="flex flex-col gap-4">
          <div className="flex flex-col gap-1.5">
            <Label className="text-xs text-muted-foreground">Distributor</Label>
            <AsyncSelect<DistributorDto>
              key={`distributor-${formKey}`}
              label="Distributor"
              placeholder="Search distributors..."
              fetcher={fetchDistributors}
              value={distributor?.id.toString() ?? ''}
              onChange={handleDistributorChange}
              getOptionValue={(d) => d.id.toString()}
              getDisplayValue={(d) => (
                <span className="flex items-center gap-2 text-sm">
                  {d.name}
                  {!d.isActive && <Badge variant="outline" className="text-[10px]">Closed</Badge>}
                </span>
              )}
              renderOption={(d) => (
                <div className="flex flex-col gap-0.5 py-0.5">
                  <span className="flex items-center gap-2 text-sm font-medium">
                    {d.name}
                    {!d.isActive && <Badge variant="outline" className="text-[10px]">Closed</Badge>}
                  </span>
                  {d.phone && <span className="text-xs text-muted-foreground">{d.phone}</span>}
                </div>
              )}
              notFound={
                <div className="py-4 text-center text-sm text-muted-foreground">
                  No distributors found
                </div>
              }
              noResultsMessage="No distributors found"
              width="320px"
              triggerClassName="h-9"
              disabled={isPending}
              clearable
            />
          </div>

          {!distributor ? (
            <div className="flex flex-col items-center justify-center gap-2 rounded-lg border border-dashed py-12 text-center">
              <ClipboardEdit className="h-8 w-8 text-muted-foreground/40" />
              <p className="text-sm text-muted-foreground">Select a distributor to load its stock</p>
            </div>
          ) : stockLoading ? (
            <div className="flex flex-col gap-2">
              {Array.from({ length: 5 }).map((_, i) => (
                <Skeleton key={i} className="h-10 w-full" />
              ))}
            </div>
          ) : stockError ? (
            <p className="text-sm text-destructive">Could not load stock for this distributor.</p>
          ) : (
            <>
              <div className="flex flex-wrap items-end gap-3 rounded-lg border bg-muted/30 px-3 py-3">
                <div className="flex flex-col gap-1.5">
                  <Label className="text-xs text-muted-foreground">Add product</Label>
                  <AsyncSelect<ProductLookupDto>
                    key={`product-${addKey}`}
                    label="Product"
                    placeholder="Search products..."
                    fetcher={fetchProducts}
                    value={addProduct?.id.toString() ?? ''}
                    onChange={handleProductChange}
                    getOptionValue={(p) => p.id.toString()}
                    getDisplayValue={(p) => (
                      <span className="text-sm">
                        <span className="font-mono text-xs">{p.code}</span> {p.itemDescription}
                      </span>
                    )}
                    renderOption={(p) => (
                      <div className="flex flex-col gap-0.5 py-0.5">
                        <span className="text-sm font-medium">{p.itemDescription}</span>
                        <span className="font-mono text-xs text-muted-foreground">{p.code}</span>
                      </div>
                    )}
                    notFound={
                      <div className="py-4 text-center text-sm text-muted-foreground">
                        No products found
                      </div>
                    }
                    noResultsMessage="No products found"
                    width="320px"
                    triggerClassName="h-9"
                    disabled={isPending}
                    clearable
                  />
                </div>
                <div className="flex flex-col gap-1.5">
                  <Label className="text-xs text-muted-foreground">Stock type</Label>
                  <Select
                    value={addStockType}
                    onValueChange={(v) => setAddStockType(v as StockType)}
                    disabled={isPending}
                  >
                    <SelectTrigger className="h-9 w-36 text-sm">
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="Normal">Normal</SelectItem>
                      <SelectItem value="FreeIssue">Free Issue</SelectItem>
                    </SelectContent>
                  </Select>
                </div>
                <Button
                  variant="outline"
                  className="h-9 gap-1.5"
                  onClick={handleAddProduct}
                  disabled={!addProduct || isDuplicate || isPending}
                >
                  <Plus className="h-3.5 w-3.5" />
                  Add
                </Button>
                {isDuplicate && (
                  <span className="text-xs text-destructive">This product and type is already listed.</span>
                )}
                <div className="relative ml-auto">
                  <Search className="absolute left-2.5 top-2.5 h-3.5 w-3.5 text-muted-foreground" />
                  <Input
                    placeholder="Filter lines..."
                    value={lineFilter}
                    onChange={(e) => setLineFilter(e.target.value)}
                    className="h-9 w-56 pl-8"
                  />
                </div>
              </div>

              {rows.length === 0 ? (
                <div className="flex flex-col items-center justify-center gap-2 rounded-lg border border-dashed py-12 text-center">
                  <p className="text-sm font-medium text-muted-foreground">
                    {distributor.name} has no stock on hand. Add a product to set a balance.
                  </p>
                </div>
              ) : visibleRows.length === 0 ? (
                <p className="py-6 text-center text-sm text-muted-foreground">No lines match the filter.</p>
              ) : (
                <AdjustmentLinesTable
                  rows={visibleRows}
                  newQuantities={newQuantities}
                  disabled={isPending}
                  onQuantityChange={(key, pieces) => setNewQuantities((prev) => ({ ...prev, [key]: pieces }))}
                  onRemove={handleRemove}
                />
              )}
            </>
          )}
        </CardContent>
      </Card>

      {/* ── 2. Reason + submit ──────────────────────────────────────────── */}
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <ClipboardEdit className="h-4 w-4" />
            Reason
          </CardTitle>
          <CardDescription>Every adjustment is recorded with its reason for the audit trail.</CardDescription>
        </CardHeader>
        <CardContent className="flex flex-col gap-4">
          <div className="flex flex-col gap-1.5">
            <Label className="text-xs text-muted-foreground">Reason</Label>
            <Select
              value={reason}
              onValueChange={(v) => setReason(v as StockAdjustmentReason)}
              disabled={isPending}
            >
              <SelectTrigger className="h-9 w-64 text-sm">
                <SelectValue placeholder="Select a reason" />
              </SelectTrigger>
              <SelectContent>
                {STOCK_ADJUSTMENT_REASONS.map((r) => (
                  <SelectItem key={r.value} value={r.value}>{r.label}</SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>

          <div className="flex flex-col gap-1.5">
            <Label htmlFor="stock-adjustment-notes" className="text-xs text-muted-foreground">
              Notes {notesRequired ? '(required)' : '(optional)'}
            </Label>
            <Textarea
              id="stock-adjustment-notes"
              placeholder="Details of the adjustment, reference, etc."
              value={notes}
              maxLength={NOTES_MAX}
              onChange={(e) => setNotes(e.target.value)}
              disabled={isPending}
              aria-invalid={notesMissing || undefined}
              className="max-w-2xl"
              rows={3}
            />
          </div>

          <div className="flex items-center gap-3">
            <Button onClick={openConfirm} disabled={!canSubmit} className="gap-2">
              <ClipboardEdit className="h-4 w-4" />
              Adjust Stock
            </Button>
            {submitHint && <span className="text-xs text-muted-foreground">{submitHint}</span>}
          </div>
        </CardContent>
      </Card>

      {/* ── 3. History ──────────────────────────────────────────────────── */}
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <History className="h-4 w-4" />
            Adjustment history
          </CardTitle>
        </CardHeader>
        <CardContent>
          <StockAdjustmentHistoryTable />
        </CardContent>
      </Card>

      <StockAdjustmentConfirmDialog
        open={confirmOpen}
        distributorName={distributor?.name ?? ''}
        reasonLabel={reason ? reasonLabel(reason) : ''}
        notes={notes}
        lines={changedLines}
        isPending={isPending}
        onOpenChange={setConfirmOpen}
        onConfirm={submit}
      />
    </div>
  )
}
