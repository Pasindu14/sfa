'use client'

import { useCallback } from 'react'
import Link from 'next/link'
import { useForm, useFieldArray, Controller } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { Plus, Trash2 } from 'lucide-react'
import { useQuery } from '@tanstack/react-query'
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
import { getDistributorsAction } from '@/features/distributor/actions/distributor.actions'
import { useDefaultPricingStructure, useCreateSalesOrder } from '../../hooks/sales-order.hooks'
import {
  createSalesOrderSchema,
  type CreateSalesOrderInput,
} from '../../schema/sales-order.schema'

// ── Helpers ────────────────────────────────────────────────────────────────

function formatCurrency(amount: number) {
  return new Intl.NumberFormat('en-LK', {
    style: 'currency',
    currency: 'LKR',
    minimumFractionDigits: 2,
  }).format(amount)
}

// ── Distributor query ─────────────────────────────────────────────────────

function useDistributors() {
  return useQuery({
    queryKey: ['distributors', 'active-all'],
    queryFn: async () => {
      const result = await getDistributorsAction(1, 1000)
      if (!result.success) throw new Error(result.error)
      return result.data.distributors.filter((d) => d.isActive)
    },
    staleTime: 5 * 60 * 1000,
  })
}

// ── Create Page ────────────────────────────────────────────────────────────

export function SalesOrderCreatePage() {
  const { data: products, isLoading: isLoadingProducts } = useAllActiveProducts()
  const { data: pricing, isLoading: isLoadingPricing } = useDefaultPricingStructure()
  const { data: distributors, isLoading: isLoadingDistributors } = useDistributors()
  const { mutate: createOrder, isPending, fieldErrors } = useCreateSalesOrder()

  const form = useForm<CreateSalesOrderInput>({
    resolver: zodResolver(createSalesOrderSchema),
    defaultValues: {
      distributorId: null,
      notes: '',
      items: [{ productId: 0, quantity: 1, unitPrice: 0, discount: 0 }],
    },
  })

  const { fields, append, remove } = useFieldArray({
    control: form.control,
    name: 'items',
  })

  const isLoading = isLoadingProducts || isLoadingPricing || isLoadingDistributors

  const getUnitPrice = useCallback(
    (productId: number): number => {
      if (!pricing?.items) return 0
      const item = pricing.items.find((i) => i.productId === productId)
      return item?.dealerCasePrice ?? item?.dealerPackPrice ?? 0
    },
    [pricing]
  )

  const handleProductChange = (index: number, productId: number) => {
    form.setValue(`items.${index}.productId`, productId)
    form.setValue(`items.${index}.unitPrice`, getUnitPrice(productId))
  }

  const watchedItems = form.watch('items')

  const subtotal = watchedItems.reduce((sum, item) => {
    const line = (item.unitPrice ?? 0) * (item.quantity ?? 0)
    return sum + line * (1 - (item.discount ?? 0) / 100)
  }, 0)

  const onSubmit = (data: CreateSalesOrderInput) => {
    createOrder({ ...data, items: data.items.filter((i) => i.productId > 0) })
  }

  if (isLoading) {
    return (
      <div className="flex items-center justify-center min-h-[400px]">
        <Spinner className="size-8" />
      </div>
    )
  }

  return (
    <div className="flex flex-col gap-6 p-6">

      {/* Header */}
      <div>
        <h1 className="text-3xl font-bold tracking-tight">Create Sales Order</h1>
        <p className="text-muted-foreground text-sm mt-1">Add line items and submit for approval</p>
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
                  <FormField
                    control={form.control}
                    name="distributorId"
                    render={({ field }) => (
                      <FormItem>
                        <FormLabel>
                          Distributor{' '}
                          <span className="text-muted-foreground text-xs font-normal">
                            (Admin only — leave blank if Distributor)
                          </span>
                        </FormLabel>
                        <Select
                          value={field.value ? String(field.value) : ''}
                          onValueChange={(v) => field.onChange(v ? Number(v) : null)}
                        >
                          <FormControl>
                            <SelectTrigger>
                              <SelectValue placeholder="Select distributor..." />
                            </SelectTrigger>
                          </FormControl>
                          <SelectContent>
                            {distributors?.map((d) => (
                              <SelectItem key={d.id} value={String(d.id)}>
                                {d.name}
                              </SelectItem>
                            ))}
                          </SelectContent>
                        </Select>
                        <FormMessage />
                        {fieldErrors?.distributorId && (
                          <p className="text-sm text-destructive">{fieldErrors.distributorId}</p>
                        )}
                      </FormItem>
                    )}
                  />

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

                          {/* Unit Price */}
                          <Controller
                            control={form.control}
                            name={`items.${index}.unitPrice`}
                            render={({ field: f }) => (
                              <Input
                                type="number"
                                step="0.01"
                                min={0}
                                className="h-8 text-sm text-right"
                                value={f.value}
                                onChange={(e) => f.onChange(Number(e.target.value))}
                              />
                            )}
                          />

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
            </div>

            {/* Right sidebar */}
            <div className="space-y-4 lg:sticky lg:top-6">
              <Card>
                <CardHeader className="pb-2">
                  <CardTitle className="text-base">Order Summary</CardTitle>
                </CardHeader>
                <CardContent className="space-y-3 text-sm">
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
                    Prices are pre-filled from the default pricing structure. You can adjust them manually.
                  </p>
                </CardContent>
              </Card>

              <Button type="submit" className="w-full" disabled={isPending}>
                {isPending && <Spinner className="mr-2" />}
                Create Sales Order
              </Button>

              <Button type="button" variant="outline" className="w-full" asChild>
                <Link href="/sales-orders">Cancel</Link>
              </Button>
            </div>
          </div>
        </form>
      </Form>
    </div>
  )
}
