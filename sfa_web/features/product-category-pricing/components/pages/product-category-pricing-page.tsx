'use client'

import { useState, useEffect } from 'react'
import {
  useProductCategoryPricings,
  useBulkUpsertProductCategoryPricings,
} from '../../hooks/product-category-pricing.hooks'
import type { ProductCategoryPricingRow } from '../../schema/product-category-pricing.schema'
import { Input } from '@/components/ui/input'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { Spinner } from '@/components/ui/spinner'

type PriceFields = { priceA: number; priceB: number; priceC: number; priceD: number }
type EditState = Record<number, PriceFields>

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

  // Initialise edit state whenever fresh data arrives (e.g. after save)
  useEffect(() => {
    if (rows.length > 0) {
      const initial: EditState = {}
      rows.forEach((r) => {
        initial[r.productId] = {
          priceA: r.priceA,
          priceB: r.priceB,
          priceC: r.priceC,
          priceD: r.priceD,
        }
      })
      setEdits(initial)
    }
  }, [rows])

  const updatePrice = (
    productId: number,
    field: keyof PriceFields,
    value: string
  ) => {
    setEdits((prev) => ({
      ...prev,
      [productId]: { ...prev[productId], [field]: parseFloat(value) || 0 },
    }))
  }

  const dirtyCount = rows.filter((r) => {
    const edit = edits[r.productId]
    return edit && isDirty(r, edit)
  }).length

  const handleSaveAll = () => {
    const items = rows.map((r) => ({
      productId: r.productId,
      ...(edits[r.productId] ?? {
        priceA: r.priceA,
        priceB: r.priceB,
        priceC: r.priceC,
        priceD: r.priceD,
      }),
    }))
    bulkUpsert(items)
  }

  return (
    <div className="flex flex-col gap-6 p-6">
      {/* Page header card */}
      <div className="flex items-center justify-between bg-muted/90 p-10 rounded-lg">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Product Category Pricing</h1>
          <p className="text-muted-foreground">
            Set prices per distributor category (A / B / C / D) for each product.
          </p>
        </div>
        {dirtyCount > 0 && (
          <Badge variant="secondary" className="text-sm px-3 py-1">
            {dirtyCount} unsaved change{dirtyCount !== 1 ? 's' : ''}
          </Badge>
        )}
      </div>

      {/* Loading state */}
      {isLoading && (
        <div className="flex items-center justify-center py-24">
          <Spinner className="size-8" />
        </div>
      )}

      {/* Scrollable grid */}
      {!isLoading && <div className="flex-1 overflow-auto">
        {rows.length === 0 ? (
          <div className="flex items-center justify-center h-40 text-muted-foreground text-sm">
            No active products found. Add products first.
          </div>
        ) : (
          <table className="w-full text-sm border-collapse">
            <thead className="sticky top-0 z-10">
              <tr className="border-b bg-muted/80 backdrop-blur">
                <th className="text-left px-3 py-2.5 font-medium w-28">Code</th>
                <th className="text-left px-3 py-2.5 font-medium">Item Description</th>
                <th className="text-right px-3 py-2.5 font-medium w-36">
                  <span className="inline-flex items-center gap-1">
                    <Badge variant="outline" className="text-xs py-0">A</Badge>
                    Price
                  </span>
                </th>
                <th className="text-right px-3 py-2.5 font-medium w-36">
                  <span className="inline-flex items-center gap-1">
                    <Badge variant="outline" className="text-xs py-0">B</Badge>
                    Price
                  </span>
                </th>
                <th className="text-right px-3 py-2.5 font-medium w-36">
                  <span className="inline-flex items-center gap-1">
                    <Badge variant="outline" className="text-xs py-0">C</Badge>
                    Price
                  </span>
                </th>
                <th className="text-right px-3 py-2.5 font-medium w-36">
                  <span className="inline-flex items-center gap-1">
                    <Badge variant="outline" className="text-xs py-0">D</Badge>
                    Price
                  </span>
                </th>
              </tr>
            </thead>
            <tbody>
              {rows.map((row) => {
                const edit = edits[row.productId]
                const dirty = edit ? isDirty(row, edit) : false
                return (
                  <tr
                    key={row.productId}
                    className={`border-b transition-colors hover:bg-muted/30 ${
                      dirty ? 'border-l-2 border-l-primary bg-primary/5' : ''
                    }`}
                  >
                    <td className="px-3 py-1.5 font-mono text-xs text-muted-foreground">
                      {row.productCode}
                    </td>
                    <td className="px-3 py-1.5 font-medium">{row.itemDescription}</td>
                    {(['priceA', 'priceB', 'priceC', 'priceD'] as const).map((field) => (
                      <td key={field} className="px-3 py-1.5">
                        <Input
                          type="number"
                          min="0"
                          step="0.01"
                          className="text-right h-8 w-full"
                          value={edit?.[field] ?? row[field]}
                          onChange={(e) => updatePrice(row.productId, field, e.target.value)}
                        />
                      </td>
                    ))}
                  </tr>
                )
              })}
            </tbody>
          </table>
        )}
      </div>}

      {/* Bottom action bar */}
      {!isLoading && (
        <div className="flex items-center justify-between border-t pt-4">
          <span className="text-sm text-muted-foreground">
            {rows.length} product{rows.length !== 1 ? 's' : ''}
            {dirtyCount > 0 && (
              <span className="text-primary font-medium">
                {' '}· {dirtyCount} unsaved change{dirtyCount !== 1 ? 's' : ''}
              </span>
            )}
          </span>
          <Button onClick={handleSaveAll} disabled={isPending || rows.length === 0}>
            {isPending ? (
              <>
                <Spinner className="mr-2" />
                Saving...
              </>
            ) : (
              'Save All'
            )}
          </Button>
        </div>
      )}
    </div>
  )
}
