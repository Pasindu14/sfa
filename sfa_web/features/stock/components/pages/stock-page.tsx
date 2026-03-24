'use client'

import { useState } from 'react'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { Badge } from '@/components/ui/badge'
import { ScrollArea } from '@/components/ui/scroll-area'
import { Skeleton } from '@/components/ui/skeleton'
import { useDistributorStock } from '../../hooks/stock.hooks'
import { useDistributorsForSelect } from '@/features/distributor/hooks/distributor.hooks'
import type { DistributorStockItem } from '../../schema/stock.schema'

// ── Stock level badge ──────────────────────────────────────────────────────

function StockLevelBadge({ qty }: { qty: number }) {
  if (qty <= 0) {
    return <Badge variant="destructive" className="text-xs">Out of Stock</Badge>
  }
  if (qty < 10) {
    return <Badge className="bg-amber-500 hover:bg-amber-600 text-white text-xs">Low ({qty})</Badge>
  }
  return <Badge variant="default" className="bg-green-600 hover:bg-green-700 text-xs">{qty}</Badge>
}

// ── Page component ─────────────────────────────────────────────────────────

export function StockPage() {
  const [selectedDistributorId, setSelectedDistributorId] = useState<number | null>(null)
  const [search, setSearch] = useState('')

  const { distributors, isLoading: distributorsLoading } = useDistributorsForSelect()
  const { data: stockItems, isLoading: stockLoading } = useDistributorStock(selectedDistributorId)

  const filteredItems: DistributorStockItem[] = (stockItems ?? []).filter((item) => {
    if (!search.trim()) return true
    const q = search.toLowerCase()
    return (
      item.productCode.toLowerCase().includes(q) ||
      item.productDescription.toLowerCase().includes(q)
    )
  })

  function handleDistributorChange(val: string) {
    setSelectedDistributorId(Number(val))
    setSearch('')
  }

  return (
    <div className="flex flex-col gap-6 p-6">
      {/* Header */}
      <div className="flex items-center justify-between bg-muted/90 p-10 rounded-lg">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Stock Dashboard</h1>
          <p className="text-muted-foreground">
            View current stock levels per distributor
          </p>
        </div>
      </div>

      {/* Distributor selector */}
      <div className="flex items-center gap-4 flex-wrap">
        <div className="flex flex-col gap-1">
          <label className="text-sm font-medium">Select Distributor</label>
          <Select
            value={selectedDistributorId?.toString() ?? ''}
            onValueChange={handleDistributorChange}
            disabled={distributorsLoading}
          >
            <SelectTrigger className="w-72">
              <SelectValue placeholder={distributorsLoading ? 'Loading...' : 'Choose a distributor'} />
            </SelectTrigger>
            <SelectContent>
              {distributors.map((d) => (
                <SelectItem key={d.id} value={d.id.toString()}>
                  {d.name}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>

        {selectedDistributorId && stockItems && (
          <>
            <div className="flex flex-col gap-1">
              <label className="text-sm font-medium">Filter Products</label>
              <input
                className="h-9 rounded-md border border-input bg-background px-3 py-1 text-sm shadow-sm placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring w-64"
                placeholder="Search by code or description..."
                value={search}
                onChange={(e) => setSearch(e.target.value)}
              />
            </div>
            <div className="flex flex-col gap-1 ml-auto">
              <span className="text-xs text-muted-foreground">Total products</span>
              <span className="text-2xl font-bold">{filteredItems.length}</span>
            </div>
          </>
        )}
      </div>

      {/* Stock table or empty state */}
      {!selectedDistributorId ? (
        <div className="flex items-center justify-center rounded-lg border border-dashed py-24 text-sm text-muted-foreground">
          Select a distributor to view their stock levels.
        </div>
      ) : (
        <div className="rounded-md border">
          <ScrollArea className="h-[calc(100vh-360px)]">
            <table className="w-full text-sm">
              <thead className="sticky top-0 bg-muted/80 backdrop-blur z-10">
                <tr>
                  <th className="text-left px-4 py-3 font-medium">Product Code</th>
                  <th className="text-left px-4 py-3 font-medium">Description</th>
                  <th className="text-right px-4 py-3 font-medium">Qty on Hand</th>
                  <th className="text-left px-4 py-3 font-medium">Status</th>
                  <th className="text-left px-4 py-3 font-medium">Last Updated</th>
                </tr>
              </thead>
              <tbody>
                {stockLoading ? (
                  Array.from({ length: 8 }).map((_, i) => (
                    <tr key={i} className="border-t">
                      {Array.from({ length: 5 }).map((_, j) => (
                        <td key={j} className="px-4 py-3">
                          <Skeleton className="h-4 w-full" />
                        </td>
                      ))}
                    </tr>
                  ))
                ) : filteredItems.length === 0 ? (
                  <tr>
                    <td colSpan={5} className="px-4 py-16 text-center text-muted-foreground">
                      {search
                        ? 'No products match your search.'
                        : 'No stock data found for this distributor.'}
                    </td>
                  </tr>
                ) : (
                  filteredItems.map((item) => (
                    <tr key={item.id} className="border-t hover:bg-muted/40 transition-colors">
                      <td className="px-4 py-3 font-mono text-xs font-medium">{item.productCode}</td>
                      <td className="px-4 py-3">{item.productDescription}</td>
                      <td className="px-4 py-3 text-right font-medium tabular-nums">{item.quantityOnHand}</td>
                      <td className="px-4 py-3">
                        <StockLevelBadge qty={item.quantityOnHand} />
                      </td>
                      <td className="px-4 py-3 text-muted-foreground text-xs">
                        {new Date(item.lastUpdatedAt).toLocaleString()}
                      </td>
                    </tr>
                  ))
                )}
              </tbody>
            </table>
          </ScrollArea>
        </div>
      )}
    </div>
  )
}
