'use client'

import { useMemo, useRef, useState } from 'react'
import {
  ChevronLeft,
  ChevronRight,
  ChevronsLeft,
  ChevronsRight,
  RotateCcw,
  Search,
} from 'lucide-react'
import {
  useProductCategoryPricings,
  useBulkUpsertProductCategoryPricings,
} from '../../hooks/product-category-pricing.hooks'
import type { ProductCategoryPricingRow } from '../../schema/product-category-pricing.schema'
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

// ── Tier language ──────────────────────────────────────────────────────────
// A/B/C/D colours mirror CATEGORY_STYLES on the distributor dashboard so a tier
// reads the same everywhere in the app. Classes are spelled out in full because
// Tailwind only picks up literal strings.

type TierKey = 'priceA' | 'priceB' | 'priceC' | 'priceD'

const TIERS: {
  key: TierKey
  letter: string
  rail: string
  badge: string
  ring: string
  cell: string
}[] = [
  {
    key: 'priceA',
    letter: 'A',
    rail: 'border-t-amber-400',
    badge:
      'bg-amber-100 text-amber-800 border-amber-200 dark:bg-amber-900/40 dark:text-amber-300 dark:border-amber-700',
    ring: 'border-amber-400 ring-2 ring-amber-400/25',
    cell: 'text-amber-700 dark:text-amber-300',
  },
  {
    key: 'priceB',
    letter: 'B',
    rail: 'border-t-zinc-400',
    badge:
      'bg-zinc-100 text-zinc-700 border-zinc-200 dark:bg-zinc-800 dark:text-zinc-300 dark:border-zinc-600',
    ring: 'border-zinc-400 ring-2 ring-zinc-400/25',
    cell: 'text-zinc-700 dark:text-zinc-300',
  },
  {
    key: 'priceC',
    letter: 'C',
    rail: 'border-t-orange-400',
    badge:
      'bg-orange-100 text-orange-700 border-orange-200 dark:bg-orange-900/40 dark:text-orange-300 dark:border-orange-700',
    ring: 'border-orange-400 ring-2 ring-orange-400/25',
    cell: 'text-orange-700 dark:text-orange-300',
  },
  {
    key: 'priceD',
    letter: 'D',
    rail: 'border-t-blue-400',
    badge:
      'bg-blue-100 text-blue-700 border-blue-200 dark:bg-blue-900/40 dark:text-blue-300 dark:border-blue-700',
    ring: 'border-blue-400 ring-2 ring-blue-400/25',
    cell: 'text-blue-700 dark:text-blue-300',
  },
]

const PAGE_SIZE_OPTIONS = [10, 25, 50, 100]

const money = new Intl.NumberFormat('en-LK', {
  minimumFractionDigits: 2,
  maximumFractionDigits: 2,
})

// ── Draft state ────────────────────────────────────────────────────────────
// Drafts are stored per *cell* as the raw input string, not as a parsed number.
// Two reasons: the user can clear a field and retype without it snapping to 0
// mid-keystroke, and knowing which individual cell changed is what lets each
// one show its own "was" value.

type RowDraft = Partial<Record<TierKey, string>>
type DraftState = Record<number, RowDraft>

function toNumber(raw: string): number {
  const n = Number.parseFloat(raw)
  return Number.isFinite(n) && n >= 0 ? n : 0
}

function isCellDirty(row: ProductCategoryPricingRow, key: TierKey, drafts: DraftState) {
  const raw = drafts[row.productId]?.[key]
  return raw !== undefined && toNumber(raw) !== row[key]
}

function isRowDirty(row: ProductCategoryPricingRow, drafts: DraftState) {
  return TIERS.some((t) => isCellDirty(row, t.key, drafts))
}

/** A product nobody has priced yet — every tier still sits at zero. */
function isUnpriced(row: ProductCategoryPricingRow) {
  return TIERS.every((t) => row[t.key] === 0)
}

type ViewMode = 'all' | 'edited' | 'unpriced'

export function ProductCategoryPricingPage() {
  const { data: rows = [], isLoading } = useProductCategoryPricings()
  const { mutate: bulkUpsert, isPending } = useBulkUpsertProductCategoryPricings()

  const [drafts, setDrafts] = useState<DraftState>({})
  const [search, setSearch] = useState('')
  const [mode, setMode] = useState<ViewMode>('all')
  const [page, setPage] = useState(0)
  const [pageSize, setPageSize] = useState(25)

  const gridRef = useRef<HTMLTableSectionElement>(null)

  const dirtyRows = useMemo(
    () => rows.filter((r) => isRowDirty(r, drafts)),
    [rows, drafts],
  )
  const unpricedCount = useMemo(() => rows.filter(isUnpriced).length, [rows])
  const hasChanges = dirtyRows.length > 0

  // Search + view filter. Both are pure view state — they never touch `drafts`
  // (keyed by productId), so unsaved edits survive filtering and paging.
  const filteredRows = useMemo(() => {
    const q = search.trim().toLowerCase()
    return rows.filter((r) => {
      if (q && !r.productCode.toLowerCase().includes(q) && !r.itemDescription.toLowerCase().includes(q))
        return false
      // "Edited" matches rows that have a draft at all, not rows that currently
      // differ — otherwise a row would vanish the instant you typed the original
      // value back, yanking the field out from under the cursor.
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

  const setCell = (productId: number, key: TierKey, raw: string) =>
    setDrafts((prev) => ({ ...prev, [productId]: { ...prev[productId], [key]: raw } }))

  const revertRow = (productId: number) =>
    setDrafts((prev) => {
      const next = { ...prev }
      delete next[productId]
      return next
    })

  const priceOf = (row: ProductCategoryPricingRow, key: TierKey) => {
    const raw = drafts[row.productId]?.[key]
    return raw === undefined ? row[key] : toNumber(raw)
  }

  const handleSave = () => {
    if (dirtyRows.length === 0) return
    // Only changed rows go over the wire — the endpoint upserts on
    // (productId, category), so untouched products don't need resending.
    bulkUpsert(
      dirtyRows.map((r) => ({
        productId: r.productId,
        priceA: priceOf(r, 'priceA'),
        priceB: priceOf(r, 'priceB'),
        priceC: priceOf(r, 'priceC'),
        priceD: priceOf(r, 'priceD'),
      })),
      { onSuccess: () => setDrafts({}) },
    )
  }

  // ── Grid keyboard navigation ─────────────────────────────────────────────
  // Up/Down/Enter walk a tier column like a spreadsheet. Number inputs would
  // otherwise spend the arrow keys on their own 0.01 steppers, which is useless
  // on a price field and costs the navigation that actually matters here.

  const moveFocus = (rowIndex: number, key: TierKey, delta: number) => {
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
    key: TierKey,
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

  return (
    <div className="flex flex-col gap-4 p-4 md:gap-6 md:p-6">
      {/* Page header */}
      <div className="flex flex-col gap-2 sm:flex-row sm:items-center sm:justify-between bg-muted/90 p-6 md:p-10 rounded-lg">
        <div>
          <h1 className="text-2xl font-bold tracking-tight md:text-3xl">
            Product Category Pricing
          </h1>
          <p className="text-sm text-muted-foreground mt-0.5">
            Set prices per distributor category (A / B / C / D) for each
            product.
          </p>
        </div>
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
            {filteredRows.length} shown · prices in LKR
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
                {TIERS.map((t) => (
                  <Skeleton key={t.key} className="h-9 w-[110px] shrink-0" />
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
              title="No active products yet"
              hint="Add products in the catalogue, then set their tier prices here."
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
            /* Horizontal scroll only — the page owns vertical scrolling, same as
               every other list in the app. Note this rules out a sticky header:
               a sticky row pins against the scrollport that scrolls it, and this
               container never scrolls vertically. The tier rails and letter
               badges carry the column identity instead. */
            <div className="overflow-x-auto">
              <table className="w-full border-separate border-spacing-0 text-sm" style={{ minWidth: 760 }}>
                <colgroup>
                  <col style={{ width: 44 }} />
                  <col style={{ width: 120 }} />
                  <col />
                  {TIERS.map((t) => (
                    <col key={t.key} style={{ width: 132 }} />
                  ))}
                </colgroup>

                <thead>
                  <tr>
                    <th className="border-b bg-card px-2 py-2.5" />
                    <th className="border-b bg-card px-4 py-2.5 text-left text-[10px] font-bold uppercase tracking-[0.15em] text-muted-foreground">
                      Code
                    </th>
                    <th className="border-b bg-card px-4 py-2.5 text-left text-[10px] font-bold uppercase tracking-[0.15em] text-muted-foreground">
                      Item
                    </th>
                    {TIERS.map((tier) => (
                      <th
                        key={tier.key}
                        className={cn(
                          'border-b border-t-[3px] bg-card px-3 py-2 text-center',
                          tier.rail,
                        )}
                      >
                        <span className="inline-flex items-center gap-1.5">
                          <span
                            className={cn(
                              'inline-flex size-5 items-center justify-center rounded-[5px] border text-[11px] font-bold',
                              tier.badge,
                            )}
                          >
                            {tier.letter}
                          </span>
                          <span className="text-[10px] font-bold uppercase tracking-[0.15em] text-muted-foreground">
                            Price
                          </span>
                        </span>
                      </th>
                    ))}
                  </tr>
                </thead>

                <tbody ref={gridRef} className="group/grid">
                  {pagedRows.map((row, rowIndex) => {
                    const rowDirty = isRowDirty(row, drafts)
                    return (
                      <tr
                        key={row.productId}
                        className="transition-colors last:[&>td]:border-b-0 hover:bg-muted/30"
                      >
                        {/* Gutter: dirty marker doubling as the row's revert control */}
                        <td
                          className={cn(
                            'border-b px-2 py-2 align-top',
                            rowDirty && 'bg-primary/[0.04]',
                          )}
                        >
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

                        <td
                          className={cn(
                            'border-b px-4 py-2 align-top',
                            rowDirty && 'bg-primary/[0.04]',
                          )}
                        >
                          <span className="inline-flex h-9 items-center font-mono text-xs tracking-tight text-muted-foreground">
                            {row.productCode}
                          </span>
                        </td>

                        <td
                          className={cn(
                            'border-b px-4 py-2 align-top',
                            rowDirty && 'bg-primary/[0.04]',
                          )}
                        >
                          <span className="flex h-9 items-center text-sm font-medium leading-snug">
                            <span className="line-clamp-2">{row.itemDescription}</span>
                          </span>
                        </td>

                        {TIERS.map((tier) => {
                          const draft = drafts[row.productId]?.[tier.key]
                          const dirty = isCellDirty(row, tier.key, drafts)
                          const unset = !dirty && row[tier.key] === 0
                          return (
                            <td
                              key={tier.key}
                              className={cn(
                                'border-b px-2 py-2 align-top',
                                rowDirty && 'bg-primary/[0.04]',
                              )}
                            >
                              <Input
                                type="number"
                                min="0"
                                step="0.01"
                                inputMode="decimal"
                                data-row={rowIndex}
                                data-field={tier.key}
                                aria-label={`Tier ${tier.letter} price for ${row.productCode}`}
                                className={cn(
                                  'h-9 w-full text-center font-mono text-sm tabular-nums',
                                  dirty && cn('font-semibold', tier.ring, tier.cell),
                                  unset && 'text-muted-foreground/60',
                                )}
                                value={draft ?? String(row[tier.key])}
                                onChange={(e) => setCell(row.productId, tier.key, e.target.value)}
                                onFocus={(e) => e.currentTarget.select()}
                                onKeyDown={(e) => handleCellKeyDown(e, rowIndex, tier.key)}
                                // Stop a page scroll over a focused field from
                                // silently stepping the price.
                                onWheel={(e) => e.currentTarget.blur()}
                              />
                              {/* Height is always reserved so revealing a diff
                                  never shifts the grid under the cursor. */}
                              <p
                                className={cn(
                                  'mt-1 h-3.5 text-center font-mono text-[10px] leading-[14px] tabular-nums',
                                  dirty ? 'text-muted-foreground' : 'invisible',
                                )}
                              >
                                was {money.format(row[tier.key])}
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
      {/* Always present once the worksheet has loaded, so Save is somewhere
          predictable. It reports the edit count rather than just sitting there,
          and Save/Discard stay disabled until there is something to act on. */}
      {!isLoading && rows.length > 0 && (
        <div className="pointer-events-none sticky bottom-5 z-20 flex justify-center">
          <div className="pointer-events-auto flex items-center gap-3 rounded-full border bg-card/95 py-2 pl-5 pr-2 shadow-lg backdrop-blur">
            <span className="flex items-center gap-2 text-sm">
              <span
                className={cn(
                  'size-1.5 rounded-full',
                  hasChanges ? 'bg-primary' : 'bg-muted-foreground/40',
                )}
              />
              {hasChanges ? (
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
              disabled={!hasChanges || isPending}
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
