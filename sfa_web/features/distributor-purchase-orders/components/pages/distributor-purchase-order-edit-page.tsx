'use client'

import { useCallback, useEffect } from 'react'
import Link from 'next/link'
import { useRouter } from 'next/navigation'
import { useForm, useFieldArray, useWatch } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { ArrowLeft, Minus, Plus, Save, ShoppingCart, Trash2 } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Textarea } from '@/components/ui/textarea'
import { Spinner } from '@/components/ui/spinner'
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
import {
  useMyPurchaseOrder,
  useMyProductCategoryPricings,
  useMyDistributorProfile,
  useUpdateMyPurchaseOrder,
} from '../../hooks/distributor-purchase-order.hooks'
import {
  updateMyPurchaseOrderSchema,
  type UpdateMyPurchaseOrderInput,
} from '../../schema/distributor-purchase-order.schema'

function formatCurrency(amount: number) {
  return new Intl.NumberFormat('en-LK', {
    style: 'currency',
    currency: 'LKR',
    minimumFractionDigits: 2,
  }).format(amount)
}

interface Props {
  id: number
}

export function DistributorPurchaseOrderEditPage({ id }: Props) {
  const router = useRouter()

  const { data: order, isLoading: isLoadingOrder } = useMyPurchaseOrder(id)
  const { data: categoryPricings = [], isLoading: isLoadingPricings } = useMyProductCategoryPricings()
  const products = categoryPricings.map((r) => ({ id: r.productId, itemDescription: r.itemDescription }))
  const { data: profile } = useMyDistributorProfile()

  const { mutate: updateOrder, isPending, fieldErrors } = useUpdateMyPurchaseOrder(
    id,
    () => router.push(`/distributor-purchase-orders/${id}`)
  )

  const form = useForm<UpdateMyPurchaseOrderInput>({
    resolver: zodResolver(updateMyPurchaseOrderSchema),
    defaultValues: { notes: '', items: [] },
  })

  const { fields, append, remove } = useFieldArray({
    control: form.control,
    name: 'items',
  })

  const watchedItems = useWatch({ control: form.control, name: 'items' })

  // Populate form once order loads
  useEffect(() => {
    if (!order) return
    form.reset({
      notes: order.notes ?? '',
      items: order.items.map((i) => ({
        productId: i.productId,
        quantity: i.quantity,
        unitPrice: i.unitPrice,
        discount: i.discount,
      })),
    })
  }, [order, form])

  // Guard — redirect if order is not Draft
  useEffect(() => {
    if (order && order.status !== 'Draft') {
      router.replace(`/distributor-purchase-orders/${id}`)
    }
  }, [order, id, router])

  const getCategoryPrice = useCallback(
    (productId: number): number => {
      if (!profile?.category || !productId) return 0
      const row = categoryPricings.find((r) => r.productId === productId)
      if (!row) return 0
      const fieldMap: Record<string, keyof typeof row> = {
        A: 'priceA', B: 'priceB', C: 'priceC', D: 'priceD',
      }
      return (row[fieldMap[profile.category]] as number) ?? 0
    },
    [categoryPricings, profile?.category]
  )

  const handleProductChange = useCallback((index: number, productId: number) => {
    form.setValue(`items.${index}.productId`, productId)
    const existingPrice = form.getValues(`items.${index}.unitPrice`)
    if (!existingPrice || existingPrice === 0) {
      form.setValue(`items.${index}.unitPrice`, getCategoryPrice(productId))
    }
  }, [form, getCategoryPrice])

  const subtotal = watchedItems.reduce((sum, item) => {
    const line = (item.unitPrice ?? 0) * (item.quantity ?? 0)
    return sum + line * (1 - (item.discount ?? 0) / 100)
  }, 0)

  const onSave = (data: UpdateMyPurchaseOrderInput) => {
    updateOrder({ ...data, items: data.items.filter((i) => i.productId > 0) })
  }

  const isLoading = isLoadingOrder || isLoadingPricings

  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-24">
        <Spinner className="size-4" />
      </div>
    )
  }

  if (!order) return null

  return (
    <div className="flex flex-col gap-6 p-6">
      {/* Header */}
      <div className="flex items-center gap-4">
        <Link href={`/distributor-purchase-orders/${id}`}>
          <Button variant="ghost" size="icon" className="h-8 w-8">
            <ArrowLeft className="h-4 w-4" />
          </Button>
        </Link>
        <div>
          <h1 className="text-xl font-bold">Edit Order — {order.orderNumber}</h1>
          <p className="text-sm text-muted-foreground">Editing Draft order</p>
        </div>
      </div>

      <Form {...form}>
        <form onSubmit={form.handleSubmit(onSave)}>
          <div className="grid grid-cols-1 lg:grid-cols-[1fr_260px] gap-6 items-start">

            {/* Left column */}
            <div className="space-y-6">
              <Card>
                <CardHeader className="pb-3">
                  <CardTitle className="text-base">Order Details</CardTitle>
                </CardHeader>
                <CardContent>
                  <FormField
                    control={form.control}
                    name="notes"
                    render={({ field }) => (
                      <FormItem>
                        <FormLabel>Notes <span className="text-muted-foreground text-xs">(optional)</span></FormLabel>
                        <FormControl>
                          <Textarea
                            placeholder="Additional instructions or notes..."
                            rows={3}
                            maxLength={1000}
                            {...field}
                          />
                        </FormControl>
                        <FormMessage />
                      </FormItem>
                    )}
                  />
                </CardContent>
              </Card>

              <Card>
                <CardHeader className="pb-3">
                  <div className="flex items-center justify-between">
                    <div className="flex items-center gap-2">
                      <div className="flex h-8 w-8 items-center justify-center rounded-md bg-blue-100 text-blue-600">
                        <ShoppingCart className="h-4 w-4" />
                      </div>
                      <CardTitle className="text-base">Order Items</CardTitle>
                    </div>
                    <Button
                      type="button"
                      variant="outline"
                      size="sm"
                      onClick={() => append({ productId: 0, quantity: 1, unitPrice: 0, discount: 0 })}
                    >
                      <Plus className="h-3.5 w-3.5 mr-1" />
                      Add Product
                    </Button>
                  </div>
                </CardHeader>
                <CardContent>
                  {fieldErrors?.items && (
                    <p className="text-sm text-destructive mb-3">{fieldErrors.items}</p>
                  )}
                  <div className="overflow-x-auto">
                    <table className="w-full text-sm">
                      <thead>
                        <tr className="border-b text-xs text-muted-foreground">
                          <th className="pb-2 text-left w-6">#</th>
                          <th className="pb-2 text-left">Product</th>
                          <th className="pb-2 text-right w-24">QTY</th>
                          <th className="pb-2 text-right w-28">Unit Price</th>
                          <th className="pb-2 text-right w-20">Disc %</th>
                          <th className="pb-2 text-right w-28">Line Total</th>
                          <th className="pb-2 w-8" />
                        </tr>
                      </thead>
                      <tbody className="divide-y">
                        {fields.map((field, index) => {
                          const item = watchedItems[index]
                          const lineTotal = (item?.unitPrice ?? 0) * (item?.quantity ?? 0) * (1 - (item?.discount ?? 0) / 100)
                          return (
                            <tr key={field.id}>
                              <td className="py-2.5 pr-2 text-muted-foreground text-xs">{index + 1}</td>
                              <td className="py-2.5 pr-2">
                                <Select
                                  value={item?.productId ? String(item.productId) : ''}
                                  onValueChange={(v) => handleProductChange(index, Number(v))}
                                >
                                  <SelectTrigger className="h-8 min-w-[180px]">
                                    <SelectValue placeholder="Select product..." />
                                  </SelectTrigger>
                                  <SelectContent>
                                    {products?.map((p) => (
                                      <SelectItem key={p.id} value={String(p.id)}>
                                        {p.itemDescription}
                                      </SelectItem>
                                    ))}
                                  </SelectContent>
                                </Select>
                              </td>
                              <td className="py-2.5 pr-2">
                                <div className="flex items-center justify-end gap-1">
                                  <Button
                                    type="button"
                                    variant="outline"
                                    size="icon"
                                    className="h-7 w-7"
                                    onClick={() => {
                                      const cur = form.getValues(`items.${index}.quantity`)
                                      if (cur > 1) form.setValue(`items.${index}.quantity`, cur - 1)
                                    }}
                                  >
                                    <Minus className="h-3 w-3" />
                                  </Button>
                                  <Input
                                    type="number"
                                    min={1}
                                    className="h-7 w-14 text-center"
                                    {...form.register(`items.${index}.quantity`, { valueAsNumber: true })}
                                  />
                                  <Button
                                    type="button"
                                    variant="outline"
                                    size="icon"
                                    className="h-7 w-7"
                                    onClick={() => {
                                      const cur = form.getValues(`items.${index}.quantity`)
                                      form.setValue(`items.${index}.quantity`, cur + 1)
                                    }}
                                  >
                                    <Plus className="h-3 w-3" />
                                  </Button>
                                </div>
                              </td>
                              <td className="py-2.5 pr-2">
                                <Input
                                  type="number"
                                  min={0}
                                  step="0.01"
                                  className="h-7 w-24 text-right ml-auto"
                                  {...form.register(`items.${index}.unitPrice`, { valueAsNumber: true })}
                                />
                              </td>
                              <td className="py-2.5 pr-2">
                                <Input
                                  type="number"
                                  min={0}
                                  max={100}
                                  className="h-7 w-16 text-right ml-auto"
                                  {...form.register(`items.${index}.discount`, { valueAsNumber: true })}
                                />
                              </td>
                              <td className="py-2.5 pr-2 text-right tabular-nums font-medium">
                                {formatCurrency(lineTotal)}
                              </td>
                              <td className="py-2.5">
                                <Button
                                  type="button"
                                  variant="ghost"
                                  size="icon"
                                  className="h-7 w-7 text-muted-foreground hover:text-destructive"
                                  onClick={() => remove(index)}
                                  disabled={fields.length === 1}
                                >
                                  <Trash2 className="h-3.5 w-3.5" />
                                </Button>
                              </td>
                            </tr>
                          )
                        })}
                      </tbody>
                    </table>
                  </div>
                </CardContent>
              </Card>
            </div>

            {/* Right column */}
            <div className="space-y-4">
              <Card>
                <CardHeader className="pb-2">
                  <CardTitle className="text-sm">Order Summary</CardTitle>
                </CardHeader>
                <CardContent className="space-y-3 text-sm">
                  <div className="flex justify-between">
                    <span className="text-muted-foreground">Order #</span>
                    <span className="font-mono font-semibold">{order.orderNumber}</span>
                  </div>
                  <div className="flex justify-between">
                    <span className="text-muted-foreground">Items</span>
                    <span>{fields.length}</span>
                  </div>
                  <div className="flex justify-between border-t pt-3">
                    <span className="font-semibold">Subtotal</span>
                    <span className="font-bold tabular-nums">{formatCurrency(subtotal)}</span>
                  </div>
                </CardContent>
              </Card>

              <div className="flex flex-col gap-2">
                <Button type="submit" disabled={isPending} className="w-full gap-2">
                  {isPending ? <Spinner className="size-3.5" /> : <Save className="h-3.5 w-3.5" />}
                  Save Changes
                </Button>
                <Link href={`/distributor-purchase-orders/${id}`} className="w-full">
                  <Button type="button" variant="outline" className="w-full">
                    Cancel
                  </Button>
                </Link>
              </div>
            </div>
          </div>
        </form>
      </Form>
    </div>
  )
}
