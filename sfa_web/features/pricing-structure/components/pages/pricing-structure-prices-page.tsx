'use client'

import { useMemo, useRef, useState } from 'react'
import Link from 'next/link'
import {
  ArrowLeft,
  ChevronLeft,
  ChevronRight,
  ChevronsLeft,
  ChevronsRight,
  RotateCcw,
  Search,
} from 'lucide-react'
import {
  usePricingStructure,
  usePricingStructureItems,
  useUpdatePricingStructureItems,
} from '../../hooks/pricing-structure.hooks'
import { MAX_PRICE, MIN_PRICE } from '../../schema/pricing-structure.schema'
import type { PricingStructureItemRow } from '../types/pricing-structure.types'
import { DefaultBadge, StatusBadge } from '../columns/pricing-structure-columns'
import { Input } from '@/components/ui/input'
import { Button } from '@/components/ui/button'
import { Skeleton } from '@/components/ui/skeleton'
import { Spinner } from '@/components/ui/spinner'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { cn } from '@/lib/utils'

// ── Price columns ──────────────────────────────────────────────────────────

type PriceKey = 'dealerPackPrice' | 'dealerCasePrice' | 'mrp'

const FIELDS: { key: PriceKey; label: string }[] = [
  { key: 'dealerPackPrice', label: 'Pack price' },
  { key: 'dealerCasePrice', label: 'Case price' },
  { key: 'mrp', label: 'MRP' },
]

const PAGE_SIZE_OPTIONS = [10, 25, 50, 100]
const MAX_ITEMS_PER_SAVE = 5000 // PUT /items accepts 1–5000 rows

const money = new Intl.NumberFormat('en-LK', {
  minimumFractionDigits: 2,
  maximumFractionDigits: 2,
})

// ── Draft state ────────────────────────────────────────────────────────────
// Drafts are stored per *cell* as the raw input string, not as a parsed number, so a field
// can be cleared and retyped without snapping, and each changed cell can show its own "was"
// value. A blank cell means "not priced" (null), never zero.

type RowDraft = Partial<Record<PriceKey, string>>
type DraftState = Record<number, RowDraft>

/** Parsed cell value: a number, null (blank = unpriced), or NaN for unparseable input. */
function parse(raw: string): number | null {
  const t = raw.trim()
  if (t === '') return null
  const n = Number(t)
  return Number.isFinite(n) ? n : Number.NaN
}

function valueOf(row: PricingStructureItemRow, key: PriceKey, drafts: DraftState) {
  const raw = drafts[row.productId]?.[key]
  return raw === undefined ? row[key] : parse(raw)
}

function isCellDirty(row: PricingStructureItemRow, key: PriceKey, drafts: DraftState) {
  const raw = drafts[row.productId]?.[key]
  if (raw === undefined) return false
  const v = parse(raw)
  return Number.isNaN(v) || v !== row[key]
}

function isRowDirty(row: PricingStructureItemRow, drafts: DraftState) {
  return FIELDS.some((f) => isCellDirty(row, f.key, drafts))
}

/** Not priced in this structure — no pack price. */
function isUnpriced(row: PricingStructureItemRow) {
  return row.dealerPackPrice === null
}

/**
 * Mirrors the API rules for one row: each price 0.01–1,000,000 when present, and a case
 * price or MRP needs a pack price. Returns a message per offending cell.
 */
function rowErrors(row: PricingStructureItemRow, drafts: DraftState) {
  const errors: Partial<Record<PriceKey, string>> = {}
  const values = {} as Record<PriceKey, number | null>
  for (const f of FIELDS) {
    const v = valueOf(row, f.key, drafts)
    values[f.key] = v
    if (v === null) continue
    if (Number.isNaN(v)) errors[f.key] = 'Not a number'
    else if (v < MIN_PRICE || v > MAX_PRICE) errors[f.key] = '0.01 – 1,000,000'
  }
  if (
    values.dealerPackPrice === null &&
    (values.dealerCasePrice !== null || values.mrp !== null)
  ) {
    errors.dealerPackPrice ??= 'Pack price required'
  }
  return errors
}

type ViewMode = 'all' | 'edited' | 'unpriced'

interface PricingStructurePricesPageProps {
  structureId: number
}

export function PricingStructurePricesPage({ structureId }: PricingStructurePricesPageProps) {
  const { data: structure, isLoading: isLoadingStructure, error } = usePricingStructure(structureId)
  const { data: rows = [], isLoading: isLoadingRows } = usePricingStructureItems(structureId)
  const { mutate: saveItems, isPending } = useUpdatePricingStructureItems(structureId)

  const [drafts, setDrafts] = useState<DraftState>({})
  const [search, setSearch] = useState('')
  const [mode, setMode] = useState<ViewMode>('all')
  const [page, setPage] = useState(0)
  const [pageSize, setPageSize] = useState(25)

  const gridRef = useRef<HTMLTableSectionElement>(null)

  const isLoading = isLoadingStructure || isLoadingRows

  const dirtyRows = useMemo(() => rows.filter((r) => isRowDirty(r, drafts)), [rows, drafts])
  // Only edited rows are validated — saved data already passed the API's rules.
  const errorsByProduct = useMemo(() => {
    const map = new Map<number, Partial<Record<PriceKey, string>>>()
    for (const r of dirtyRows) {
      const e = rowErrors(r, drafts)
      if (Object.keys(e).length > 0) map.set(r.productId, e)
    }
    return map
  }, [dirtyRows, drafts])
  const unpricedCount = useMemo(() => rows.filter(isUnpriced).length, [rows])
  const hasChanges = dirtyRows.length > 0
  const invalidCount = errorsByProduct.size
  const tooMany = dirtyRows.length > MAX_ITEMS_PER_SAVE

  // Search + view filter. Both are pure view state — they never touch `drafts` (keyed by
  // productId), so unsaved edits survive filtering and paging.
  const filteredRows = useMemo(() => {
    const q = search.trim().toLowerCase()
    return rows.filter((r) => {
      if (
        q &&
        !r.productCode.toLowerCase().includes(q) &&
        !r.itemDescription.toLowerCase().includes(q)
      )
        return false
      // "Edited" matches rows that have a draft at all, not rows that currently differ —
      // otherwise a row would vanish the instant you typed the original value back.
      if (mode === 'edited') return drafts[r.productId] !== undefined
      if (mode === 'unpriced') return isUnpriced(r)
      return true
    })
  }, [rows, search, mode, drafts])

  const pageCount = Math.max(1, Math.ceil(filteredRows.length / pageSize))
  const safePage = Math.min(page, pageCount - 1) // a shrinking filter can strand `page` past the end
  const pagedRows = useMemo(
    () => filteredRows.slice(safePage * pageSize, safePage * pageSize + pageSize),
    [filteredRows, safePage, pageSize],
  )

  // ── Mutations on draft state ─────────────────────────────────────────────

  const setCell = (productId: number, key: PriceKey, raw: string) =>
    setDrafts((prev) => ({ ...prev, [productId]: { ...prev[productId], [key]: raw } }))

  const revertRow = (productId: number) =>
    setDrafts((prev) => {
      const next = { ...prev }
      delete next[productId]
      return next
    })

  const handleSave = () => {
    if (!hasChanges || invalidCount > 0 || tooMany) return
    // Only changed rows go over the wire; every row sends all three prices because the
    // endpoint replaces the row (null clears a price).
    saveItems(
      dirtyRows.map((r) => ({
        productId: r.productId,
        dealerPackPrice: valueOf(r, 'dealerPackPrice', drafts),
        dealerCasePrice: valueOf(r, 'dealerCasePrice', drafts),
        mrp: valueOf(r, 'mrp', drafts),
      })),
      { onSuccess: () => setDrafts({}) },
    )
  }

  // ── Grid keyboard navigation ─────────────────────────────────────────────
  // Up/Down/Enter walk a price column like a spreadsheet. Number inputs would otherwise
  // spend the arrow keys on their own 0.01 steppers.

  const moveFocus = (rowIndex: number, key: PriceKey, delta: number) => {
    const next = gridRef.current?.querySelector<HTMLInputElement>(
      `input[data-row="${rowIndex + delta}"][data-field="${key}"]`,
    )
    if (!next) return
    next.focus()
    next.select()
  }

  const handleCellKeyDown = (
    e: React.KeyboardEvent<HTMLInputElement>,
    rowIndex: number,
    key: PriceKey,
  ) => {
    if (e.key === 'ArrowDown' || e.key === 'Enter') {
      e.preventDefault()
      moveFocus(rowIndex, key, 1)
    } else if (e.key === 'ArrowUp') {
      e.preventDefault()
      moveFocus(rowIndex, key, -1)
    }
  }

  const resetPaging = () => setPage(0)

  if (!isLoadingStructure && !structure) {
    return (
      <div className="flex flex-col gap-4 p-4 md:gap-6 md:p-6">
        <BackLink />
        <EmptyState
          title="Pricing structure not found"
          hint={error ? error.message : 'It may have been deleted.'}
        />
      </div>
    )
  }

  return (
    <div className="flex flex-col gap-4 p-4 md:gap-6 md:p-6">
      {/* Page header */}
      <div className="flex flex-col gap-3 rounded-lg bg-muted/90 p-6 md:p-10">
        <BackLink />
        {structure ? (
          <div>
            <div className="flex flex-wrap items-center gap-2">
              <h1 className="text-2xl font-bold tracking-tight md:text-3xl">{structure.name}</h1>
              {structure.isDefault && <DefaultBadge />}
              <StatusBadge isActive={structure.isActive} />
            </div>
            <p className="mt-0.5 text-sm text-muted-foreground">
              {structure.description ||
                'Set the pack price, case price and MRP for each product. Leave a price blank to exclude the product from this list.'}
            </p>
          </div>
        ) : (
          <Skeleton className="h-9 w-64" />
        )}
      </div>

      {/* ── Toolbar ──────────────────────────────────────────────────────── */}
      {!isLoading && rows.length > 0 && (
        <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
          <div className="flex flex-col gap-3 sm:flex-row sm:items-center">
            <div className="relative w-full sm:w-72">
              <Search className="pointer-events-none absolute left-3 top-1/2 size-4 -translate-y-1/2 text-muted-foreground" />
              <Input
                type="search"
                placeholder="Search code or description"
                className="h-9 pl-9"
                value={search}
                onChange={(e) => {
                  setSearch(e.target.value)
                  resetPaging()
                }}
              />
            </div>

            <div className="flex items-center gap-1 rounded-lg border bg-muted/40 p-1">
              <ViewTab
                label="All"
                count={rows.length}
                active={mode === 'all'}
                onClick={() => {
                  setMode('all')
                  resetPaging()
                }}
              />
              <ViewTab
                label="Edited"
                count={dirtyRows.length}
                active={mode === 'edited'}
                onClick={() => {
                  setMode('edited')
                  resetPaging()
                }}
              />
              <ViewTab
                label="Unpriced"
                count={unpricedCount}
                active={mode === 'unpriced'}
                onClick={() => {
                  setMode('unpriced')
                  resetPaging()
                }}
              />
            </div>
          </div>

          <p className="text-xs text-muted-foreground">
            {filteredRows.length} shown · prices in LKR · blank = not priced
          </p>
        </div>
      )}

      {/* ── Loading ──────────────────────────────────────────────────────── */}
      {isLoading && (
        <div className="overflow-hidden rounded-xl border bg-card shadow-sm">
          <div className="border-b bg-muted/40 px-4 py-3">
            <Skeleton className="h-4 w-40" />
          </div>
          <div className="divide-y">
            {Array.from({ length: 8 }).map((_, i) => (
              <div key={i} className="flex items-center gap-4 px-4 py-4">
                <Skeleton className="h-5 w-20 shrink-0" />
                <Skeleton className="h-4 flex-1" />
                {FIELDS.map((f) => (
                  <Skeleton key={f.key} className="h-9 w-[110px] shrink-0" />
                ))}
              </div>
            ))}
          </div>
        </div>
      )}

      {/* ── Worksheet ────────────────────────────────────────────────────── */}
      {!isLoading && (
        <div className="overflow-hidden rounded-xl border bg-card shadow-sm">
          {rows.length === 0 ? (
            <EmptyState
              title="No products yet"
              hint="Add products in the catalogue, then price them here."
            />
          ) : filteredRows.length === 0 ? (
            <EmptyState
              title={
                mode === 'edited'
                  ? 'Nothing edited yet'
                  : mode === 'unpriced'
                    ? 'Every product has a price'
                    : 'No products match your search'
              }
              hint={
                mode === 'edited'
                  ? 'Change a price and it will show up here for review.'
                  : mode === 'unpriced'
                    ? 'Switch to All to browse the full catalogue.'
                    : 'Try a different code or description.'
              }
            />
          ) : (
            /* Horizontal scroll only — the page owns vertical scrolling, same as the
               category pricing worksheet, which rules out a sticky header. */
            <div className="overflow-x-auto">
              <table
                className="w-full border-separate border-spacing-0 text-sm"
                style={{ minWidth: 720 }}
              >
                <colgroup>
                  <col style={{ width: 44 }} />
                  <col style={{ width: 120 }} />
                  <col />
                  {FIELDS.map((f) => (
                    <col key={f.key} style={{ width: 140 }} />
                  ))}
                </colgroup>

                <thead>
                  <tr>
                    <th className="border-b bg-card px-2 py-2.5" />
                    <th className="border-b bg-card px-4 py-2.5 text-left text-[10px] font-bold uppercase tracking-[0.15em] text-muted-foreground">
                      Code
                    </th>
                    <th className="border-b bg-card px-4 py-2.5 text-left text-[10px] font-bold uppercase tracking-[0.15em] text-muted-foreground">
                      Description
                    </th>
                    {FIELDS.map((f) => (
                      <th
                        key={f.key}
                        className="border-b bg-card px-3 py-2.5 text-center text-[10px] font-bold uppercase tracking-[0.15em] text-muted-foreground"
                      >
                        {f.label}
                      </th>
                    ))}
                  </tr>
                </thead>

                <tbody ref={gridRef}>
                  {pagedRows.map((row, rowIndex) => {
                    const rowDirty = isRowDirty(row, drafts)
                    const errors = errorsByProduct.get(row.productId)
                    const tint = errors
                      ? 'bg-destructive/[0.04]'
                      : rowDirty
                        ? 'bg-primary/[0.04]'
                        : undefined
                    return (
                      <tr
                        key={row.productId}
                        className="transition-colors last:[&>td]:border-b-0 hover:bg-muted/30"
                      >
                        {/* Gutter: dirty marker doubling as the row's revert control */}
                        <td className={cn('border-b px-2 py-2 align-top', tint)}>
                          <div className="flex h-9 items-center justify-center">
                            {rowDirty && (
                              <Button
                                type="button"
                                variant="ghost"
                                size="icon"
                                aria-label={`Revert ${row.productCode} to saved prices`}
                                title="Revert this row"
                                className="size-7 text-primary hover:bg-primary/10 hover:text-primary"
                                onClick={() => revertRow(row.productId)}
                              >
                                <RotateCcw className="size-3.5" />
                              </Button>
                            )}
                          </div>
                        </td>

                        <td className={cn('border-b px-4 py-2 align-top', tint)}>
                          <span className="inline-flex h-9 items-center font-mono text-xs tracking-tight text-muted-foreground">
                            {row.productCode}
                          </span>
                        </td>

                        <td className={cn('border-b px-4 py-2 align-top', tint)}>
                          <div className="flex min-h-9 flex-col justify-center">
                            <span className="line-clamp-2 text-sm font-medium leading-snug">
                              {row.itemDescription}
                            </span>
                            {!row.isProductActive && (
                              <span className="text-[10px] font-medium text-amber-600">
                                Inactive product
                              </span>
                            )}
                          </div>
                        </td>

                        {FIELDS.map((f) => {
                          const draft = drafts[row.productId]?.[f.key]
                          const dirty = isCellDirty(row, f.key, drafts)
                          const error = errors?.[f.key]
                          const saved = row[f.key]
                          return (
                            <td key={f.key} className={cn('border-b px-2 py-2 align-top', tint)}>
                              <Input
                                type="number"
                                min={MIN_PRICE}
                                max={MAX_PRICE}
                                step="0.01"
                                inputMode="decimal"
                                placeholder="—"
                                data-row={rowIndex}
                                data-field={f.key}
                                aria-label={`${f.label} for ${row.productCode}`}
                                aria-invalid={error ? true : undefined}
                                className={cn(
                                  'h-9 w-full text-center font-mono text-sm tabular-nums',
                                  dirty && 'border-primary font-semibold ring-2 ring-primary/20',
                                  error && 'border-destructive ring-destructive/20',
                                )}
                                value={draft ?? (saved === null ? '' : String(saved))}
                                onChange={(e) => setCell(row.productId, f.key, e.target.value)}
                                onFocus={(e) => e.currentTarget.select()}
                                onKeyDown={(e) => handleCellKeyDown(e, rowIndex, f.key)}
                                // Stop a page scroll over a focused field from silently
                                // stepping the price.
                                onWheel={(e) => e.currentTarget.blur()}
                              />
                              {/* Height is always reserved so revealing a hint never shifts
                                  the grid under the cursor. An error wins over "was". */}
                              <p
                                className={cn(
                                  'mt-1 h-3.5 text-center text-[10px] leading-[14px] tabular-nums',
                                  error
                                    ? 'text-destructive'
                                    : dirty
                                      ? 'font-mono text-muted-foreground'
                                      : 'invisible',
                                )}
                              >
                                {error ??
                                  `was ${saved === null ? 'unpriced' : money.format(saved)}`}
                              </p>
                            </td>
                          )
                        })}
                      </tr>
                    )
                  })}
                </tbody>
              </table>
            </div>
          )}
        </div>
      )}

      {/* ── Pagination ───────────────────────────────────────────────────── */}
      {!isLoading && filteredRows.length > 0 && (
        <div className="flex flex-col items-center justify-between gap-4 sm:flex-row">
          <p className="text-sm text-muted-foreground">
            Page {safePage + 1} of {pageCount}
          </p>
          <div className="flex items-center gap-4">
            <div className="flex items-center gap-2">
              <span className="whitespace-nowrap text-xs font-medium text-muted-foreground">
                Rows per page
              </span>
              <Select
                value={`${pageSize}`}
                onValueChange={(value) => {
                  setPageSize(Number.parseInt(value, 10))
                  resetPaging()
                }}
              >
                <SelectTrigger size="sm" className="w-[72px] cursor-pointer">
                  <SelectValue placeholder={pageSize} />
                </SelectTrigger>
                <SelectContent side="top">
                  {PAGE_SIZE_OPTIONS.map((size) => (
                    <SelectItem key={size} value={`${size}`} className="cursor-pointer">
                      {size}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
            <div className="flex items-center gap-1">
              <PagerButton
                label="Go to first page"
                onClick={() => setPage(0)}
                disabled={safePage === 0}
                className="hidden lg:inline-flex"
              >
                <ChevronsLeft className="size-4" />
              </PagerButton>
              <PagerButton
                label="Go to previous page"
                onClick={() => setPage((p) => Math.max(0, p - 1))}
                disabled={safePage === 0}
              >
                <ChevronLeft className="size-4" />
              </PagerButton>
              <PagerButton
                label="Go to next page"
                onClick={() => setPage((p) => Math.min(pageCount - 1, p + 1))}
                disabled={safePage >= pageCount - 1}
              >
                <ChevronRight className="size-4" />
              </PagerButton>
              <PagerButton
                label="Go to last page"
                onClick={() => setPage(pageCount - 1)}
                disabled={safePage >= pageCount - 1}
                className="hidden lg:inline-flex"
              >
                <ChevronsRight className="size-4" />
              </PagerButton>
            </div>
          </div>
        </div>
      )}

      {/* ── Commit dock ──────────────────────────────────────────────────── */}
      {/* Always present once the worksheet has loaded, so Save is somewhere predictable.
          Save stays disabled while any edited row breaks the price rules. */}
      {!isLoading && rows.length > 0 && (
        <div className="pointer-events-none sticky bottom-5 z-20 flex justify-center">
          <div className="pointer-events-auto flex items-center gap-3 rounded-full border bg-card/95 py-2 pl-5 pr-2 shadow-lg backdrop-blur">
            <span className="flex items-center gap-2 text-sm">
              <span
                className={cn(
                  'size-1.5 rounded-full',
                  invalidCount > 0
                    ? 'bg-destructive'
                    : hasChanges
                      ? 'bg-primary'
                      : 'bg-muted-foreground/40',
                )}
              />
              {invalidCount > 0 ? (
                <span className="text-destructive">
                  <span className="font-medium tabular-nums">{invalidCount}</span> product
                  {invalidCount !== 1 ? 's' : ''} need fixing
                </span>
              ) : tooMany ? (
                <span className="text-destructive">
                  Save at most {MAX_ITEMS_PER_SAVE.toLocaleString()} products at a time
                </span>
              ) : hasChanges ? (
                <span>
                  <span className="font-medium tabular-nums">{dirtyRows.length}</span>{' '}
                  <span className="text-muted-foreground">
                    product{dirtyRows.length !== 1 ? 's' : ''} edited
                  </span>
                </span>
              ) : (
                <span className="text-muted-foreground">No unsaved changes</span>
              )}
            </span>
            <span className="h-5 w-px bg-border" />
            <Button
              variant="ghost"
              size="sm"
              className="rounded-full text-muted-foreground"
              onClick={() => setDrafts({})}
              disabled={!hasChanges || isPending}
            >
              Discard
            </Button>
            <Button
              size="sm"
              className="rounded-full px-5"
              onClick={handleSave}
              disabled={!hasChanges || invalidCount > 0 || tooMany || isPending}
            >
              {isPending ? (
                <>
                  <Spinner className="mr-1.5" />
                  Saving
                </>
              ) : (
                'Save changes'
              )}
            </Button>
          </div>
        </div>
      )}
    </div>
  )
}

// ── Small parts ────────────────────────────────────────────────────────────

function BackLink() {
  return (
    <Link
      href="/pricing-structures"
      className="inline-flex w-fit items-center gap-1 text-sm text-muted-foreground hover:text-foreground"
    >
      <ArrowLeft className="size-4" />
      Pricing structures
    </Link>
  )
}

function ViewTab({
  label,
  count,
  active,
  onClick,
}: {
  label: string
  count: number
  active: boolean
  onClick: () => void
}) {
  return (
    <button
      type="button"
      onClick={onClick}
      aria-pressed={active}
      className={cn(
        'flex cursor-pointer items-center gap-1.5 rounded-md px-2.5 py-1 text-xs font-medium transition-colors',
        active
          ? 'bg-card text-foreground shadow-sm'
          : 'text-muted-foreground hover:text-foreground',
      )}
    >
      {label}
      <span
        className={cn(
          'rounded px-1 text-[10px] tabular-nums',
          active ? 'bg-muted text-muted-foreground' : 'text-muted-foreground/70',
        )}
      >
        {count}
      </span>
    </button>
  )
}

function PagerButton({
  label,
  onClick,
  disabled,
  className,
  children,
}: {
  label: string
  onClick: () => void
  disabled: boolean
  className?: string
  children: React.ReactNode
}) {
  return (
    <Button
      type="button"
      variant="outline"
      size="icon"
      aria-label={label}
      onClick={onClick}
      disabled={disabled}
      className={cn('size-8 cursor-pointer', className)}
    >
      {children}
    </Button>
  )
}

function EmptyState({ title, hint }: { title: string; hint: string }) {
  return (
    <div className="flex flex-col items-center justify-center gap-1.5 px-6 py-20 text-center">
      <p className="text-sm font-medium">{title}</p>
      <p className="text-xs text-muted-foreground">{hint}</p>
    </div>
  )
}
