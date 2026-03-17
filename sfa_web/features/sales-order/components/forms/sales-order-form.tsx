'use client'

import { useFieldArray, useFormContext } from 'react-hook-form'
import { Plus, Trash2 } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import {
  FormControl,
  FormField,
  FormItem,
  FormMessage,
} from '@/components/ui/form'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Spinner } from '@/components/ui/spinner'
import { formatLKR } from '@/lib/utils'
import type { PricingStructureItemDto } from '../../types/sales-order.types'
import type { CreateSalesOrderInput, UpdateSalesOrderInput } from '../../schema/sales-order.schema'

// Both Create and Update schemas share the same items shape — accept either context
type WithItems = Pick<CreateSalesOrderInput, 'items'> | Pick<UpdateSalesOrderInput, 'items'>

interface SalesOrderFormProps {
  pricingItems: PricingStructureItemDto[]
  isLoadingPricing: boolean
  pricingError: boolean
}

export function SalesOrderLineItemsForm({
  pricingItems,
  isLoadingPricing,
  pricingError,
}: SalesOrderFormProps) {
  const { control, setValue, watch } = useFormContext<WithItems>()
  const { fields, append, remove } = useFieldArray({ control, name: 'items' })

  const items = watch('items') ?? []

  function handleProductSelect(index: number, productId: string) {
    const pid = parseInt(productId)
    const pItem = pricingItems.find((p) => p.productId === pid)
    if (!pItem) return
    setValue(`items.${index}.unitPrice`, pItem.dealerCasePrice ?? 0)
    setValue(`items.${index}.discount`, 0)
  }

  function addRow() {
    append({ productId: 0, quantity: 1, unitPrice: 0, discount: 0 })
  }

  return (
    <Card>
      <CardHeader className="flex flex-row items-center justify-between">
        <CardTitle className="text-base">Line Items</CardTitle>
        <Button type="button" variant="outline" size="sm" onClick={addRow} className="gap-2">
          <Plus className="h-4 w-4" />
          Add Product
        </Button>
      </CardHeader>
      <CardContent>
        {isLoadingPricing && (
          <div className="flex justify-center py-8">
            <Spinner />
          </div>
        )}
        {pricingError && (
          <p className="text-sm text-destructive text-center py-4">
            Failed to load product catalogue. Please refresh.
          </p>
        )}
        {!isLoadingPricing && !pricingError && fields.length === 0 && (
          <p className="text-sm text-muted-foreground text-center py-4">
            No items added. Click &quot;Add Product&quot; to start.
          </p>
        )}
        {!isLoadingPricing && !pricingError && fields.length > 0 && (
          <div className="space-y-2">
            <div className="grid grid-cols-[2fr_1fr_1fr_1fr_1fr_auto] gap-2 text-xs font-medium text-muted-foreground px-1">
              <span>Product</span>
              <span>SKU</span>
              <span>Qty</span>
              <span>Case Price</span>
              <span>Total</span>
              <span />
            </div>
            {fields.map((field, index) => {
              const selectedProductId = items[index]?.productId
              const selectedItem = pricingItems.find((p) => p.productId === selectedProductId)
              const qty = items[index]?.quantity ?? 0
              const unitPrice = items[index]?.unitPrice ?? 0
              const lineTotal = qty * unitPrice

              return (
                <div key={field.id} className="grid grid-cols-[2fr_1fr_1fr_1fr_1fr_auto] gap-2 items-start">
                  {/* Product selector */}
                  <FormField
                    control={control}
                    name={`items.${index}.productId`}
                    render={() => (
                      <FormItem>
                        <FormControl>
                          <Select
                            value={selectedProductId ? String(selectedProductId) : ''}
                            onValueChange={(val) => handleProductSelect(index, val)}
                          >
                            <SelectTrigger className="h-8 text-xs">
                              <SelectValue placeholder="Select product" />
                            </SelectTrigger>
                            <SelectContent>
                              {pricingItems.map((p) => (
                                <SelectItem key={p.productId} value={String(p.productId)}>
                                  {p.productItemDescription} ({p.productCode})
                                </SelectItem>
                              ))}
                            </SelectContent>
                          </Select>
                        </FormControl>
                        <FormMessage />
                      </FormItem>
                    )}
                  />
                  {/* SKU (read-only) */}
                  <Input
                    readOnly
                    value={selectedItem?.productCode ?? ''}
                    className="h-8 text-xs bg-muted font-mono"
                    tabIndex={-1}
                  />
                  {/* Quantity */}
                  <FormField
                    control={control}
                    name={`items.${index}.quantity`}
                    render={({ field: f }) => (
                      <FormItem>
                        <FormControl>
                          <Input
                            type="number"
                            min={1}
                            className="h-8 text-xs"
                            {...f}
                            onChange={(e) => f.onChange(parseInt(e.target.value) || 1)}
                          />
                        </FormControl>
                        <FormMessage />
                      </FormItem>
                    )}
                  />
                  {/* Dealer Case Price (read-only) */}
                  <Input
                    readOnly
                    value={selectedItem?.dealerCasePrice != null ? formatLKR(selectedItem.dealerCasePrice) : 'N/A'}
                    className="h-8 text-xs bg-muted"
                    tabIndex={-1}
                  />
                  {/* Line Total (read-only) */}
                  <Input
                    readOnly
                    value={selectedProductId ? formatLKR(lineTotal) : '—'}
                    className="h-8 text-xs bg-muted"
                    tabIndex={-1}
                  />
                  {/* Remove button */}
                  <Button
                    type="button"
                    variant="ghost"
                    size="icon"
                    className="h-8 w-8 text-destructive hover:text-destructive"
                    onClick={() => remove(index)}
                  >
                    <Trash2 className="h-4 w-4" />
                  </Button>
                </div>
              )
            })}
          </div>
        )}
      </CardContent>
    </Card>
  )
}
