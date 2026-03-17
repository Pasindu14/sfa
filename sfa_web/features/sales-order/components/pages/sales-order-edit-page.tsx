'use client'

import { useEffect } from 'react'
import { useRouter } from 'next/navigation'
import { useForm, FormProvider } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import {
  FormField,
  FormItem,
  FormLabel,
  FormControl,
  FormMessage,
} from '@/components/ui/form'
import { Textarea } from '@/components/ui/textarea'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Separator } from '@/components/ui/separator'
import { Spinner } from '@/components/ui/spinner'
import { formatLKR } from '@/lib/utils'
import {
  updateSalesOrderSchema,
  type UpdateSalesOrderInput,
} from '../../schema/sales-order.schema'
import {
  useSalesOrder,
  useUpdateSalesOrder,
  useDefaultPricingStructure,
} from '../../hooks/sales-order.hooks'
import { SalesOrderLineItemsForm } from '../forms/sales-order-form'

interface SalesOrderEditPageProps {
  orderId: number
}

export function SalesOrderEditPage({ orderId }: SalesOrderEditPageProps) {
  const router = useRouter()

  const { data: order, isLoading: isLoadingOrder } = useSalesOrder(orderId)
  const { data: pricing, isLoading: isLoadingPricing, isError: pricingError } =
    useDefaultPricingStructure()
  const { mutate, isPending } = useUpdateSalesOrder(orderId)

  const form = useForm<UpdateSalesOrderInput>({
    resolver: zodResolver(updateSalesOrderSchema),
    defaultValues: { notes: '', items: [] },
  })

  // Redirect if not Draft
  useEffect(() => {
    if (order && order.status !== 0) {
      router.replace(`/sales-orders/${orderId}`)
    }
  }, [order, orderId, router])

  // Pre-populate form from existing order
  useEffect(() => {
    if (order) {
      form.reset({
        notes: order.notes ?? '',
        items: order.items.map((item) => ({
          productId: item.productId,
          quantity: item.quantity,
          unitPrice: item.unitPrice,
          discount: 0 as const,
        })),
      })
    }
  }, [order, form.reset])

  const items = form.watch('items') ?? []
  const total = items.reduce((sum, item) => {
    return sum + (item.quantity * item.unitPrice)
  }, 0)

  if (isLoadingOrder) {
    return (
      <div className="flex justify-center items-center min-h-[400px]">
        <Spinner />
      </div>
    )
  }

  if (!order) {
    return (
      <div className="flex flex-col items-center gap-4 p-6">
        <p className="text-muted-foreground">Order not found.</p>
        <Button variant="outline" onClick={() => router.push('/sales-orders')}>
          ← Back to Sales Orders
        </Button>
      </div>
    )
  }

  return (
    <div className="flex flex-col gap-6 p-6">
      {/* Breadcrumb */}
      <div className="flex items-center gap-2 text-sm text-muted-foreground">
        <button onClick={() => router.push('/sales-orders')} className="hover:text-foreground">
          Sales Orders
        </button>
        <span>/</span>
        <button
          onClick={() => router.push(`/sales-orders/${orderId}`)}
          className="hover:text-foreground"
        >
          {order.orderNumber}
        </button>
        <span>/</span>
        <span className="text-foreground font-medium">Edit</span>
      </div>

      <FormProvider {...form}>
        <form
          onSubmit={form.handleSubmit((data) => mutate(data))}
          className="flex flex-row gap-6"
        >
          <div className="flex-1 flex flex-col gap-4">
            <Card>
              <CardHeader>
                <CardTitle className="text-base">Order Details</CardTitle>
              </CardHeader>
              <CardContent>
                <FormField
                  control={form.control}
                  name="notes"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>Notes (optional)</FormLabel>
                      <FormControl>
                        <Textarea
                          placeholder="Add any order notes..."
                          className="resize-none"
                          rows={3}
                          {...field}
                          value={field.value ?? ''}
                        />
                      </FormControl>
                      <FormMessage />
                    </FormItem>
                  )}
                />
              </CardContent>
            </Card>

            <SalesOrderLineItemsForm
              pricingItems={pricing?.items ?? []}
              isLoadingPricing={isLoadingPricing}
              pricingError={pricingError}
            />
          </div>

          <div className="w-80 sticky top-6 self-start">
            <Card>
              <CardHeader>
                <CardTitle className="text-base">Order Summary</CardTitle>
              </CardHeader>
              <CardContent className="flex flex-col gap-3">
                <div className="flex justify-between text-sm">
                  <span className="text-muted-foreground">Items</span>
                  <span>{items.length}</span>
                </div>
                <Separator />
                <div className="flex justify-between text-sm font-medium">
                  <span>Total</span>
                  <span>{formatLKR(total)}</span>
                </div>
                <Separator />
                <Button type="submit" disabled={isPending} className="w-full gap-2">
                  {isPending && <Spinner className="h-4 w-4" />}
                  Save Changes
                </Button>
                <Button
                  type="button"
                  variant="ghost"
                  className="w-full"
                  onClick={() => router.push(`/sales-orders/${orderId}`)}
                >
                  Cancel
                </Button>
              </CardContent>
            </Card>
          </div>
        </form>
      </FormProvider>
    </div>
  )
}
