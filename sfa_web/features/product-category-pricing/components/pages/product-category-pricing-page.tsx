'use client'

import { useMemo, useState } from "react";
import {
  ChevronLeftIcon,
  ChevronRightIcon,
  DoubleArrowLeftIcon,
  DoubleArrowRightIcon,
  MagnifyingGlassIcon,
} from "@radix-ui/react-icons";
import {
  useProductCategoryPricings,
  useBulkUpsertProductCategoryPricings,
} from '../../hooks/product-category-pricing.hooks'
import type { ProductCategoryPricingRow } from '../../schema/product-category-pricing.schema'
import { Input } from '@/components/ui/input'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { Spinner } from '@/components/ui/spinner'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'

const PAGE_SIZE_OPTIONS = [10, 20, 30, 40, 50];

type PriceFields = { priceA: number; priceB: number; priceC: number; priceD: number }

// Sparse override map — only stores rows the user has actually changed.
// Unedited rows fall back to server data in `rows`.
type EditState = Record<number, PriceFields>

const PRICE_FIELDS = [
  { key: "priceA" as const, label: "A" },
  { key: "priceB" as const, label: "B" },
  { key: "priceC" as const, label: "C" },
  { key: "priceD" as const, label: "D" },
];

function isDirty(original: ProductCategoryPricingRow, edit: PriceFields): boolean {
  return (
    original.priceA !== edit.priceA ||
    original.priceB !== edit.priceB ||
    original.priceC !== edit.priceC ||
    original.priceD !== edit.priceD
  )
}

export function ProductCategoryPricingPage() {
  const { data: rows = [], isLoading } = useProductCategoryPricings()
  const { mutate: bulkUpsert, isPending } = useBulkUpsertProductCategoryPricings()
  const [edits, setEdits] = useState<EditState>({})

  // Client-side search + pagination. These are pure *view* concerns — they never
  // touch `edits` (keyed by productId), so unsaved changes survive filtering/paging.
  const [search, setSearch] = useState("")
  const [page, setPage] = useState(0) // zero-based
  const [pageSize, setPageSize] = useState(20)

  // Filter by code or description (case-insensitive).
  const filteredRows = useMemo(() => {
    const q = search.trim().toLowerCase()
    if (!q) return rows
    return rows.filter(
      (r) =>
        r.productCode.toLowerCase().includes(q) ||
        r.itemDescription.toLowerCase().includes(q),
    )
  }, [rows, search])

  const pageCount = Math.max(1, Math.ceil(filteredRows.length / pageSize))
  // Clamp: a shrinking filter can leave `page` past the end.
  const safePage = Math.min(page, pageCount - 1)
  const pagedRows = useMemo(
    () => filteredRows.slice(safePage * pageSize, safePage * pageSize + pageSize),
    [filteredRows, safePage, pageSize],
  )

  const updatePrice = (
    productId: number,
    field: keyof PriceFields,
    value: string,
  ) => {
    const row = rows.find((r) => r.productId === productId);
    if (!row) return;
    setEdits((prev) => ({
      ...prev,
      [productId]: {
        priceA: prev[productId]?.priceA ?? row.priceA,
        priceB: prev[productId]?.priceB ?? row.priceB,
        priceC: prev[productId]?.priceC ?? row.priceC,
        priceD: prev[productId]?.priceD ?? row.priceD,
        [field]: parseFloat(value) || 0,
      },
    }));
  };

  const dirtyCount = rows.filter((r) => {
    const edit = edits[r.productId]
    return edit !== undefined && isDirty(r, edit);
  }).length

  const handleSaveAll = () => {
    const items = rows.map((r) => ({
      productId: r.productId,
      priceA: edits[r.productId]?.priceA ?? r.priceA,
      priceB: edits[r.productId]?.priceB ?? r.priceB,
      priceC: edits[r.productId]?.priceC ?? r.priceC,
      priceD: edits[r.productId]?.priceD ?? r.priceD,
    }));
    bulkUpsert(items, { onSuccess: () => setEdits({}) });
  }

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
        {dirtyCount > 0 && (
          <Badge
            variant="secondary"
            className="self-start sm:self-auto text-sm px-3 py-1 shrink-0"
          >
            {dirtyCount} unsaved change{dirtyCount !== 1 ? "s" : ""}
          </Badge>
        )}
      </div>

      {/* Search bar */}
      {!isLoading && rows.length > 0 && (
        <div className="relative w-full sm:max-w-sm">
          <MagnifyingGlassIcon className="pointer-events-none absolute left-3 top-1/2 size-4 -translate-y-1/2 text-muted-foreground" />
          <Input
            type="search"
            placeholder="Search by code or description..."
            className="pl-9"
            value={search}
            onChange={(e) => {
              setSearch(e.target.value)
              setPage(0) // jump back to first page on a new query
            }}
          />
        </div>
      )}

      {/* Loading */}
      {isLoading && (
        <div className="flex items-center justify-center py-24">
          <Spinner className="size-4" />
        </div>
      )}

      {/* Table card */}
      {!isLoading && (
        <>
          <div className="rounded-lg border bg-card shadow-sm overflow-hidden">
            {rows.length === 0 ? (
              <div className="flex flex-col items-center justify-center py-16 gap-2 text-muted-foreground">
                <p className="text-sm font-medium">No active products found.</p>
                <p className="text-xs">
                  Add products first, then set their category prices here.
                </p>
              </div>
            ) : filteredRows.length === 0 ? (
              <div className="flex flex-col items-center justify-center py-16 gap-2 text-muted-foreground">
                <p className="text-sm font-medium">No products match your search.</p>
                <p className="text-xs">Try a different code or description.</p>
              </div>
            ) : (
              /* Horizontal scroll on narrow viewports — table never compresses below 700px */
              <div className="overflow-x-auto">
                <table className="w-full text-sm" style={{ minWidth: "700px" }}>
                  <colgroup>
                    {/* Code — fixed narrow */}
                    <col style={{ width: "110px" }} />
                    {/* Description — takes remaining space */}
                    <col />
                    {/* 4 price columns — fixed equal width */}
                    <col style={{ width: "130px" }} />
                    <col style={{ width: "130px" }} />
                    <col style={{ width: "130px" }} />
                    <col style={{ width: "130px" }} />
                  </colgroup>

                  <thead>
                    <tr className="border-b bg-muted/50">
                      <th className="text-left px-4 py-3 text-xs font-semibold text-muted-foreground uppercase tracking-wider whitespace-nowrap">
                        Code
                      </th>
                      <th className="text-left px-4 py-3 text-xs font-semibold text-muted-foreground uppercase tracking-wider">
                        Item Description
                      </th>
                      {PRICE_FIELDS.map(({ label }) => (
                        <th
                          key={label}
                          className="px-3 py-3 text-xs font-semibold text-muted-foreground uppercase tracking-wider text-center"
                        >
                          <div className="flex flex-col items-center gap-1">
                            <span className="inline-flex items-center justify-center h-6 w-6 rounded-full bg-primary/10 text-primary font-bold text-xs">
                              {label}
                            </span>
                            <span>Price</span>
                          </div>
                        </th>
                      ))}
                    </tr>
                  </thead>

                  <tbody className="divide-y divide-border">
                    {pagedRows.map((row, idx) => {
                      const edit = edits[row.productId];
                      const dirty = edit ? isDirty(row, edit) : false;
                      return (
                        <tr
                          key={row.productId}
                          className={[
                            "transition-colors hover:bg-muted/40",
                            idx % 2 === 0 ? "bg-background" : "bg-muted/20",
                            dirty
                              ? "border-l-[3px] border-l-primary bg-primary/5!"
                              : "",
                          ].join(" ")}
                        >
                          {/* Code pill */}
                          <td className="px-4 py-3 align-middle">
                            <span className="inline-block font-mono text-xs bg-muted px-2 py-0.5 rounded text-muted-foreground whitespace-nowrap">
                              {row.productCode}
                            </span>
                          </td>

                          {/* Description */}
                          <td className="px-4 py-3 align-middle">
                            <span className="font-medium text-sm leading-snug line-clamp-2">
                              {row.itemDescription}
                            </span>
                          </td>

                          {/* Price inputs */}
                          {PRICE_FIELDS.map(({ key }) => (
                            <td key={key} className="px-3 py-2.5 align-middle">
                              <Input
                                type="number"
                                min="0"
                                step="0.01"
                                className="text-center h-9 w-full tabular-nums font-medium"
                                value={edits[row.productId]?.[key] ?? row[key]}
                                onChange={(e) =>
                                  updatePrice(
                                    row.productId,
                                    key,
                                    e.target.value,
                                  )
                                }
                              />
                            </td>
                          ))}
                        </tr>
                      );
                    })}
                  </tbody>
                </table>
              </div>
            )}
          </div>

          {/* Pagination controls — client-side over the filtered set */}
          {filteredRows.length > 0 && (
            <div className="flex w-full flex-col items-center justify-between gap-4 px-2 sm:flex-row sm:gap-8">
              <div className="flex-1 text-sm text-muted-foreground">
                {filteredRows.length} row(s).
              </div>
              <div className="flex flex-col items-center gap-4 sm:flex-row sm:gap-6 lg:gap-8">
                <div className="flex items-center space-x-2">
                  <p className="whitespace-nowrap text-sm font-medium">
                    Rows per page
                  </p>
                  <Select
                    value={`${pageSize}`}
                    onValueChange={(value) => {
                      setPageSize(parseInt(value, 10))
                      setPage(0)
                    }}
                  >
                    <SelectTrigger className="cursor-pointer" size="sm">
                      <SelectValue placeholder={pageSize} />
                    </SelectTrigger>
                    <SelectContent side="top" className="cursor-pointer">
                      {PAGE_SIZE_OPTIONS.map((size) => (
                        <SelectItem
                          key={size}
                          value={`${size}`}
                          className="cursor-pointer"
                        >
                          {size}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </div>
                <div className="flex items-center justify-center text-sm font-medium">
                  Page {safePage + 1} of {pageCount}
                </div>
                <div className="flex items-center space-x-2">
                  <Button
                    aria-label="Go to first page"
                    variant="outline"
                    className="hidden size-8 p-0 lg:flex cursor-pointer"
                    onClick={() => setPage(0)}
                    disabled={safePage === 0}
                  >
                    <DoubleArrowLeftIcon className="h-4 w-4" aria-hidden="true" />
                  </Button>
                  <Button
                    aria-label="Go to previous page"
                    variant="outline"
                    className="size-8 p-0 cursor-pointer"
                    onClick={() => setPage((p) => Math.max(0, p - 1))}
                    disabled={safePage === 0}
                  >
                    <ChevronLeftIcon className="h-4 w-4" aria-hidden="true" />
                  </Button>
                  <Button
                    aria-label="Go to next page"
                    variant="outline"
                    className="size-8 p-0 cursor-pointer"
                    onClick={() => setPage((p) => Math.min(pageCount - 1, p + 1))}
                    disabled={safePage >= pageCount - 1}
                  >
                    <ChevronRightIcon className="h-4 w-4" aria-hidden="true" />
                  </Button>
                  <Button
                    aria-label="Go to last page"
                    variant="outline"
                    className="hidden size-8 p-0 lg:flex cursor-pointer"
                    onClick={() => setPage(pageCount - 1)}
                    disabled={safePage >= pageCount - 1}
                  >
                    <DoubleArrowRightIcon className="h-4 w-4" aria-hidden="true" />
                  </Button>
                </div>
              </div>
            </div>
          )}

          {/* Bottom action bar — sticky, stacks on mobile */}
          <div className="sticky bottom-0 z-10 flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between bg-background/95 backdrop-blur border-t px-4 py-3 -mx-4 md:-mx-6">
            <div className="flex items-center gap-3 text-sm text-muted-foreground">
              <span>
                {rows.length} product{rows.length !== 1 ? "s" : ""}
              </span>
              {dirtyCount > 0 && (
                <>
                  <span>·</span>
                  <span className="text-primary font-medium">
                    {dirtyCount} unsaved change{dirtyCount !== 1 ? "s" : ""}
                  </span>
                </>
              )}
            </div>
            <Button
              size="lg"
              className="w-full sm:w-auto"
              onClick={handleSaveAll}
              disabled={isPending || rows.length === 0}
            >
              {isPending ? (
                <>
                  <Spinner className="mr-2" />
                  Saving...
                </>
              ) : (
                "Save All"
              )}
            </Button>
          </div>
        </>
      )}
    </div>
  );
}
