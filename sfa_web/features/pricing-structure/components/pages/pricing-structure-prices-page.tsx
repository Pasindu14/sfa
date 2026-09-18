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

function isNum(v: number | null): v is number {
  return v !== null && !Number.isNaN(v)
}

/**
 * The case price the phone bills when a structure leaves it blank: pack × packs per case.
 * Null when it can't be derived (no pack price, or the product has no case size).
 */
function derivedCasePrice(pack: number | null, piecesPerPack: number) {
  return isNum(pack) && piecesPerPack > 0 ? Math.round(pack * piecesPerPack * 100) / 100 : null
}

/** Outlet margin (MRP − pack price) ÷ MRP, as a percentage. Null unless both are valid. */
function marginPct(pack: number | null, mrp: number | null) {
  return isNum(pack) && isNum(mrp) && mrp > 0 ? ((mrp - pack) / mrp) * 100 : null
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
        <div className="flex flex-col gap-3 lg:flex-row lg:items-center lg:justify-between">
          <div className="flex flex-col gap-3 sm:flex-row sm:items-center">
            <div className="relative w-full sm:w-80">
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

            <div className="flex w-fit items-center gap-1 rounded-lg border bg-muted/40 p-1">
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
                tone={dirtyRows.length > 0 ? 'primary' : undefined}
                onClick={() => {
                  setMode('edited')
                  resetPaging()
                }}
              />
              <ViewTab
                label="Unpriced"
                count={unpricedCount}
                active={mode === 'unpriced'}
                tone={unpricedCount > 0 ? 'warning' : undefined}
                onClick={() => {
                  setMode('unpriced')
                  resetPaging()
                }}
              />
            </div>
          </div>

          <div className="flex flex-wrap items-center gap-x-4 gap-y-1 text-xs text-muted-foreground">
            <span>
              <span className="font-medium tabular-nums text-foreground">
                {rows.length - unpricedCount}
              </span>{' '}
              of {rows.length} priced
            </span>
            <span className="hidden h-3 w-px bg-border sm:block" />
            <span>Prices in LKR · blank = not priced</span>
            <span className="hidden h-3 w-px bg-border sm:block" />
            <span>Blank case price = pack × packs per case</span>
          </div>
        </div>
      )}

      {/* ── Loading ──────────────────────────────────────────────────────── */}
      {isLoading && (
        <div className="overflow-hidden rounded-xl border bg-card shadow-sm">
          <div className="border-b bg-muted/40 px-5 py-3">
            <Skeleton className="h-3 w-48" />
          </div>
          <div className="divide-y">
            {Array.from({ length: 8 }).map((_, i) => (
              <div key={i} className="flex items-center gap-4 px-5 py-3.5">
                <Skeleton className="h-5 w-14 shrink-0" />
                <div className="flex flex-1 flex-col gap-1.5">
                  <Skeleton className="h-4 w-2/5" />
                  <Skeleton className="h-3 w-24" />
                </div>
                {FIELDS.map((f) => (
                  <Skeleton key={f.key} className="h-8 w-32 shrink-0" />
                ))}
                <Skeleton className="h-4 w-12 shrink-0" />
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
            /* Horizontal scroll only — the page owns vertical scrolling. Columns are sized in
               percentages so the price inputs sit beside the product on wide screens instead of
               being pushed to the far edge. */
            <div className="overflow-x-auto">
              <table className="w-full table-fixed border-collapse text-sm" style={{ minWidth: 880 }}>
                <colgroup>
                  <col style={{ width: '38%' }} />
                  {FIELDS.map((f) => (
                    <col key={f.key} style={{ width: f.key === 'dealerCasePrice' ? '18%' : '15%' }} />
                  ))}
                  <col style={{ width: '9%' }} />
                  <col style={{ width: 52 }} />
                </colgroup>

                <thead>
                  <tr className="border-b bg-muted/40">
                    <th className="px-5 py-2.5 text-left text-[11px] font-semibold uppercase tracking-wider text-muted-foreground">
                      Product
                    </th>
                    {FIELDS.map((f) => (
                      <th
                        key={f.key}
                        className="px-3 py-2.5 text-right text-[11px] font-semibold uppercase tracking-wider text-muted-foreground"
                      >
                        {f.label}
                      </th>
                    ))}
                    <th
                      className="px-3 py-2.5 text-right text-[11px] font-semibold uppercase tracking-wider text-muted-foreground"
                      title="Outlet margin: (MRP − pack price) ÷ MRP"
                    >
                      Margin
                    </th>
                    <th className="px-2 py-2.5">
                      <span className="sr-only">Actions</span>
                    </th>
                  </tr>
                </thead>

                <tbody ref={gridRef} className="divide-y">
                  {pagedRows.map((row, rowIndex) => {
                    const rowDirty = isRowDirty(row, drafts)
                    const errors = errorsByProduct.get(row.productId)
                    const pack = valueOf(row, 'dealerPackPrice', drafts)
                    const kase = valueOf(row, 'dealerCasePrice', drafts)
                    const mrp = valueOf(row, 'mrp', drafts)
                    const derivedCase = derivedCasePrice(pack, row.piecesPerPack)
                    const caseMismatch =
                      derivedCase !== null && isNum(kase) && Math.abs(kase - derivedCase) >= 0.01
                    const margin = marginPct(pack, mrp)
                    const unpriced = pack === null

                    return (
                      <tr
                        key={row.productId}
                        className={cn(
                          'group transition-colors hover:bg-muted/30',
                          errors ? 'bg-destructive/[0.03]' : rowDirty && 'bg-primary/[0.03]',
                        )}
                      >
                        {/* Product — accent bar marks edited / invalid rows */}
                        <td className="relative px-5 py-3 align-middle">
                          <span
                            aria-hidden
                            className={cn(
                              'absolute inset-y-0 left-0 w-[3px]',
                              errors ? 'bg-destructive' : rowDirty ? 'bg-primary' : 'bg-transparent',
                            )}
                          />
                          <div className="flex min-w-0 items-center gap-3">
                            <span className="shrink-0 rounded-md bg-muted px-1.5 py-0.5 font-mono text-[11px] font-medium text-muted-foreground">
                              {row.productCode}
                            </span>
                            <div className="min-w-0">
                              <p className="truncate font-medium leading-snug" title={row.itemDescription}>
                                {row.itemDescription}
                              </p>
                              <div className="mt-0.5 flex items-center gap-2 text-[11px] text-muted-foreground">
                                {row.piecesPerPack > 0 && (
                                  <span className="tabular-nums">{row.piecesPerPack} pkts / case</span>
                                )}
                                {!row.isProductActive && (
                                  <span className="rounded bg-amber-500/10 px-1.5 py-px font-medium text-amber-700 dark:text-amber-400">
                                    Inactive product
                                  </span>
                                )}
                                {unpriced && row.isProductActive && (
                                  <span className="rounded bg-muted px-1.5 py-px font-medium">
                                    Not priced
                                  </span>
                                )}
                              </div>
                            </div>
                          </div>
                        </td>

                        {FIELDS.map((f) => {
                          const draft = drafts[row.productId]?.[f.key]
                          const dirty = isCellDirty(row, f.key, drafts)
                          const error = errors?.[f.key]
                          const saved = row[f.key]
                          const isCase = f.key === 'dealerCasePrice'
                          // Hint under the input, in priority order: error › "was" › case check.
                          const hint = error
                            ? { text: error, className: 'text-destructive' }
                            : dirty
                              ? {
                                  text: `was ${saved === null ? 'unpriced' : money.format(saved)}`,
                                  className: 'text-muted-foreground',
                                }
                              : isCase && caseMismatch
                                ? {
                                    text: `≠ pack × ${row.piecesPerPack} (${money.format(derivedCase!)})`,
                                    className: 'text-amber-700 dark:text-amber-400',
                                  }
                                : null
                          return (
                            <td key={f.key} className="px-3 py-3 align-middle">
                              <div className="relative ml-auto max-w-[168px]">
                                <span className="pointer-events-none absolute left-2.5 top-1/2 -translate-y-1/2 text-[11px] text-muted-foreground/70">
                                  Rs
                                </span>
                                <Input
                                  type="number"
                                  min={MIN_PRICE}
                                  max={MAX_PRICE}
                                  step="0.01"
                                  inputMode="decimal"
                                  // An empty case price is billed as pack × packs per case on the
                                  // phone — show that value so the admin sees what a blank means.
                                  placeholder={
                                    isCase && derivedCase !== null ? money.format(derivedCase) : '—'
                                  }
                                  data-row={rowIndex}
                                  data-field={f.key}
                                  aria-label={`${f.label} for ${row.productCode}`}
                                  aria-invalid={error ? true : undefined}
                                  className={cn(
                                    'h-8 w-full pl-8 pr-2.5 text-right font-mono text-[13px] tabular-nums shadow-none placeholder:text-muted-foreground/50',
                                    '[appearance:textfield] [&::-webkit-inner-spin-button]:appearance-none [&::-webkit-outer-spin-button]:appearance-none',
                                    dirty && 'border-primary bg-primary/5 font-semibold',
                                    isCase && caseMismatch && !dirty && !error && 'border-amber-400/70',
                                    error && 'border-destructive bg-destructive/5',
                                  )}
                                  value={draft ?? (saved === null ? '' : saved.toFixed(2))}
                                  onChange={(e) => setCell(row.productId, f.key, e.target.value)}
                                  onFocus={(e) => e.currentTarget.select()}
                                  onKeyDown={(e) => handleCellKeyDown(e, rowIndex, f.key)}
                                  // Stop a page scroll over a focused field from silently
                                  // stepping the price.
                                  onWheel={(e) => e.currentTarget.blur()}
                                />
                                {/* Absolutely placed in the row's padding, so a hint appearing
                                    never shifts the grid under the cursor. */}
                                {hint && (
                                  <p
                                    className={cn(
                                      'absolute right-0 top-full mt-0.5 truncate text-right text-[10px] leading-3 tabular-nums',
                                      hint.className,
                                    )}
                                    title={hint.text}
                                  >
                                    {hint.text}
                                  </p>
                                )}
                              </div>
                            </td>
                          )
                        })}

                        <td className="px-3 py-3 text-right align-middle">
                          {margin === null ? (
                            <span className="text-muted-foreground/50">—</span>
                          ) : (
                            <span
                              className={cn(
                                'font-mono text-[13px] tabular-nums',
                                margin < 0 ? 'font-semibold text-destructive' : 'text-muted-foreground',
                              )}
                              title="(MRP − pack price) ÷ MRP"
                            >
                              {margin.toFixed(1)}%
                            </span>
                          )}
                        </td>

                        <td className="px-2 py-3 align-middle">
                          {rowDirty && (
                            <Button
                              type="button"
                              variant="ghost"
                              size="icon"
                              aria-label={`Revert ${row.productCode} to saved prices`}
                              title="Revert this row"
                              className="size-7 text-muted-foreground hover:text-foreground"
                              onClick={() => revertRow(row.productId)}
                            >
                              <RotateCcw className="size-3.5" />
                            </Button>
                          )}
                        </td>
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
  tone,
  onClick,
}: {
  label: string
  count: number
  active: boolean
  /** Draws attention to a non-zero count (unsaved edits, unpriced products). */
  tone?: 'primary' | 'warning'
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
          tone === 'primary'
            ? 'bg-primary/10 font-semibold text-primary'
            : tone === 'warning'
              ? 'bg-amber-500/10 font-semibold text-amber-700 dark:text-amber-400'
              : active
                ? 'bg-muted text-muted-foreground'
                : 'text-muted-foreground/70',
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
