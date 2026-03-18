'use client'

import { useEffect, useCallback } from 'react'
import Link from 'next/link'
import { useForm, useFieldArray, useWatch, Controller } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { Plus, Trash2 } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Separator } from '@/components/ui/separator'
import { Spinner } from '@/components/ui/spinner'
import { Input } from '@/components/ui/input'
import { Textarea } from '@/components/ui/textarea'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import {
  Form,
  FormControl,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from '@/components/ui/form'
import { useAllActiveProducts } from '@/features/product/hooks/product.hooks'
import {
  usePurchaseOrder,
  useDefaultPricingStructure,
  useUpdatePurchaseOrder,
} from '../../hooks/purchase-order.hooks'
import {
  updatePurchaseOrderSchema,
  PurchaseOrderStatus,
  type UpdatePurchaseOrderInput,
} from '../../schema/purchase-order.schema'
import { formatCurrency } from '../../utils/format'

// ── Edit Page ──────────────────────────────────────────────────────────────

interface PurchaseOrderEditPageProps {
  orderId: number
}

export function PurchaseOrderEditPage({ orderId }: PurchaseOrderEditPageProps) {
  const { data: order, isLoading: isLoadingOrder } = usePurchaseOrder(orderId)
  const { data: products, isLoading: isLoadingProducts } = useAllActiveProducts()
  const { data: pricing, isLoading: isLoadingPricing } = useDefaultPricingStructure()
  const { mutate: updateOrder, isPending, fieldErrors } = useUpdatePurchaseOrder(orderId)

  const form = useForm<UpdatePurchaseOrderInput>({
    resolver: zodResolver(updatePurchaseOrderSchema),
    defaultValues: {
      notes: '',
      items: [{ productId: 0, quantity: 1, unitPrice: 0, discount: 0 }],
    },
  })

  const { fields, append, remove } = useFieldArray({
    control: form.control,
    name: 'items',
  })

  // Seed form when order loads
  useEffect(() => {
    if (order) {
      form.reset({
        notes: order.notes ?? '',
        items: order.items.map((i) => ({
          productId: i.productId,
          quantity: i.quantity,
          unitPrice: i.unitPrice,
          discount: i.discount,
        })),
      })
    }
  }, [order, form])

  const isLoading = isLoadingOrder || isLoadingProducts || isLoadingPricing

  const getPricingEntry = useCallback(
    (productId: number) => {
      if (!pricing?.items || !productId) return null
      return pricing.items.find((i) => i.productId === productId) ?? null
    },
    [pricing]
  )

  const getUnitPrice = useCallback(
    (productId: number): number => {
      const entry = getPricingEntry(productId)
      return entry?.dealerCasePrice ?? entry?.dealerPackPrice ?? 0
    },
    [getPricingEntry]
  )

  const handleProductChange = useCallback((index: number, productId: number) => {
    form.setValue(`items.${index}.productId`, productId)
    form.setValue(`items.${index}.unitPrice`, getUnitPrice(productId))
  }, [form, getUnitPrice])

  const watchedItems = useWatch({ control: form.control, name: 'items' })

  const subtotal = watchedItems.reduce((sum, item) => {
    const line = (item.unitPrice ?? 0) * (item.quantity ?? 0)
    return sum + line * (1 - (item.discount ?? 0) / 100)
  }, 0)

  const onSubmit = (data: UpdatePurchaseOrderInput) => {
    updateOrder({ ...data, items: data.items.filter((i) => i.productId > 0) })
  }

  if (isLoading) {
    return (
      <div className="flex flex-col gap-6 p-6">
        <div className="bg-muted/90 p-10 rounded-lg">
          <Spinner className="size-6" />
        </div>
      </div>
    )
  }

  // Guard: only Draft orders are editable
  if (order && order.status !== PurchaseOrderStatus.Draft) {
    return (
      <div className="flex flex-col gap-6 p-6">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Edit Purchase Order</h1>
          <p className="text-muted-foreground text-sm mt-1">
            Only Draft orders can be edited.
          </p>
        </div>
        <Button variant="outline" asChild>
          <Link href={`/purchase-orders/${orderId}`}>← Back to Order</Link>
        </Button>
      </div>
    )
  }

  return (
    <div className="flex flex-col gap-6 p-6">

      {/* Header */}
      <div>
        <h1 className="text-3xl font-bold tracking-tight">
          Edit {order?.orderNumber ?? 'Sales Order'}
        </h1>
        <p className="text-muted-foreground text-sm mt-1">
          Update items and notes — distributor cannot be changed
        </p>
      </div>

      {/* Form */}
      <Form {...form}>
        <form onSubmit={form.handleSubmit(onSubmit)}>
          <div className="grid grid-cols-1 lg:grid-cols-[1fr_300px] gap-6 items-start">

            {/* Left column */}
            <div className="space-y-6">

              {/* Order Details */}
              <Card>
                <CardHeader className="pb-3">
                  <CardTitle className="text-base">Order Details</CardTitle>
                </CardHeader>
                <CardContent className="space-y-4">
                  {/* Distributor — read-only */}
                  <div>
                    <p className="text-sm font-medium mb-1.5">Distributor</p>
                    <p className="text-sm text-muted-foreground border rounded-md px-3 py-2 bg-muted/40">
                      {order?.distributorName ?? '—'}
                    </p>
                  </div>

                  <FormField
                    control={form.control}
                    name="notes"
                    render={({ field }) => (
                      <FormItem>
                        <FormLabel>
                          Notes{' '}
                          <span className="text-muted-foreground text-xs font-normal">(optional)</span>
                        </FormLabel>
                        <FormControl>
                          <Textarea
                            {...field}
                            placeholder="Any additional notes..."
                            rows={3}
                            className="resize-none"
                          />
                        </FormControl>
                        <FormMessage />
                      </FormItem>
                    )}
                  />
                </CardContent>
              </Card>

              {/* Line Items */}
              <Card>
                <CardHeader className="pb-3 flex flex-row items-center justify-between">
                  <CardTitle className="text-base">Order Items</CardTitle>
                  <Button
                    type="button"
                    variant="outline"
                    size="sm"
                    onClick={() => append({ productId: 0, quantity: 1, unitPrice: 0, discount: 0 })}
                  >
                    <Plus className="h-4 w-4 mr-1" />
                    Add Product
                  </Button>
                </CardHeader>
                <CardContent className="p-0">
                  {/* Column headers */}
                  <div className="grid grid-cols-[1fr_80px_120px_100px_32px] gap-3 px-6 py-2 border-b bg-muted/40">
                    <span className="text-xs font-medium text-muted-foreground uppercase tracking-wide">Product</span>
                    <span className="text-xs font-medium text-muted-foreground uppercase tracking-wide">QTY</span>
                    <span className="text-xs font-medium text-muted-foreground uppercase tracking-wide">Case Price</span>
                    <span className="text-xs font-medium text-muted-foreground uppercase tracking-wide text-right">Line Total</span>
                    <span />
                  </div>

                  <div className="divide-y">
                    {fields.map((field, index) => {
                      const watchedItem = watchedItems[index]
                      const lineTotal =
                        (watchedItem?.unitPrice ?? 0) *
                        (watchedItem?.quantity ?? 0) *
                        (1 - (watchedItem?.discount ?? 0) / 100)

                      return (
                        <div
                          key={field.id}
                          className="grid grid-cols-[1fr_80px_120px_100px_32px] gap-3 px-6 py-3 items-center"
                        >
                          {/* Product select */}
                          <Controller
                            control={form.control}
                            name={`items.${index}.productId`}
                            render={({ field: f }) => (
                              <Select
                                value={f.value ? String(f.value) : ''}
                                onValueChange={(v) => handleProductChange(index, Number(v))}
                              >
                                <SelectTrigger className="h-8 text-sm">
                                  <SelectValue placeholder="Select product..." />
                                </SelectTrigger>
                                <SelectContent>
                                  {products?.map((p) => (
                                    <SelectItem key={p.id} value={String(p.id)}>
                                      <span className="font-medium">{p.itemDescription}</span>
                                      <span className="ml-2 text-xs text-muted-foreground font-mono">{p.code}</span>
                                    </SelectItem>
                                  ))}
                                </SelectContent>
                              </Select>
                            )}
                          />

                          {/* Quantity */}
                          <Controller
                            control={form.control}
                            name={`items.${index}.quantity`}
                            render={({ field: f }) => (
                              <Input
                                type="number"
                                min={1}
                                className="h-8 text-sm text-center"
                                value={f.value}
                                onChange={(e) => f.onChange(Number(e.target.value))}
                              />
                            )}
                          />

                          {/* Unit Price — read-only, from pricing structure */}
                          {(() => {
                            const pid = watchedItems[index]?.productId
                            const entry = getPricingEntry(pid)
                            const hasCasePrice = entry?.dealerCasePrice != null
                            const hasPackPrice = entry?.dealerPackPrice != null
                            const price = entry?.dealerCasePrice ?? entry?.dealerPackPrice ?? null
                            const priceLabel = hasCasePrice ? 'Case price' : hasPackPrice ? 'Pack price' : null

                            return (
                              <div className="flex flex-col items-end gap-0.5">
                                {pid && !entry ? (
                                  <span className="text-xs text-amber-600 font-medium">No pricing</span>
                                ) : price != null ? (
                                  <>
                                    <span className="text-sm font-medium tabular-nums text-right">
                                      {formatCurrency(price)}
                                    </span>
                                    {priceLabel && (
                                      <span className="text-[10px] text-muted-foreground leading-none">
                                        {priceLabel}
                                      </span>
                                    )}
                                  </>
                                ) : (
                                  <span className="text-sm text-muted-foreground">—</span>
                                )}
                              </div>
                            )
                          })()}

                          {/* Line Total */}
                          <span className="text-sm font-semibold text-right tabular-nums">
                            {formatCurrency(lineTotal)}
                          </span>

                          {/* Delete */}
                          <Button
                            type="button"
                            variant="ghost"
                            size="icon"
                            className="h-8 w-8 text-destructive hover:text-destructive"
                            onClick={() => remove(index)}
                            disabled={fields.length === 1}
                          >
                            <Trash2 className="h-4 w-4" />
                          </Button>
                        </div>
                      )
                    })}
                  </div>

                  {/* Totals */}
                  <div className="border-t px-6 py-4">
                    <div className="flex justify-between text-sm font-bold">
                      <span>Subtotal (LKR)</span>
                      <span className="tabular-nums">{formatCurrency(subtotal)}</span>
                    </div>
                  </div>
                </CardContent>
              </Card>

              {/* Field-level errors from server */}
              {fieldErrors && (
                <div className="rounded-md border border-destructive/50 bg-destructive/5 px-4 py-3 text-sm text-destructive">
                  {Object.values(fieldErrors).map((msg, i) => (
                    <p key={i}>{msg}</p>
                  ))}
                </div>
              )}
            </div>

            {/* Right sidebar */}
            <div className="space-y-4 lg:sticky lg:top-6">
              <Card>
                <CardHeader className="pb-2">
                  <CardTitle className="text-base">Order Summary</CardTitle>
                </CardHeader>
                <CardContent className="space-y-3 text-sm">
                  <div className="flex justify-between">
                    <span className="text-muted-foreground">Order #</span>
                    <span className="font-medium">{order?.orderNumber}</span>
                  </div>
                  <div className="flex justify-between">
                    <span className="text-muted-foreground">Line items</span>
                    <span className="font-medium">{fields.length}</span>
                  </div>
                  <div className="flex justify-between">
                    <span className="text-muted-foreground">Subtotal</span>
                    <span className="font-semibold tabular-nums">{formatCurrency(subtotal)}</span>
                  </div>
                  <Separator />
                  <p className="text-xs text-muted-foreground">
                    Prices are auto-filled from the default price list and cannot be edited.
                  </p>
                </CardContent>
              </Card>

              <Button type="submit" className="w-full" disabled={isPending}>
                {isPending && <Spinner className="mr-2" />}
                Save Changes
              </Button>

              <Button type="button" variant="outline" className="w-full" asChild>
                <Link href={`/purchase-orders/${orderId}`}>Cancel</Link>
              </Button>
            </div>
          </div>
        </form>
      </Form>
    </div>
  )
}
