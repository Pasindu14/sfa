'use client'

import { useSession } from 'next-auth/react'
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
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { Textarea } from '@/components/ui/textarea'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Separator } from '@/components/ui/separator'
import { Spinner } from '@/components/ui/spinner'
import { formatLKR } from '@/lib/utils'
import {
  createSalesOrderSchema,
  type CreateSalesOrderInput,
} from '../../schema/sales-order.schema'
import { useCreateSalesOrder, useDefaultPricingStructure } from '../../hooks/sales-order.hooks'
import { useDistributorsForSelect } from '@/features/distributor/hooks/distributor.hooks'
import { SalesOrderLineItemsForm } from '../forms/sales-order-form'

export function SalesOrderCreatePage() {
  const { data: session } = useSession()
  const router = useRouter()
  const role = session?.user?.role ?? ''

  const { data: pricing, isLoading: isLoadingPricing, isError: pricingError } =
    useDefaultPricingStructure()
  const { distributors, isLoading: isLoadingDistributors } = useDistributorsForSelect()

  const { mutate, isPending, fieldErrors } = useCreateSalesOrder()

  const form = useForm<CreateSalesOrderInput>({
    resolver: zodResolver(createSalesOrderSchema),
    defaultValues: {
      distributorId: null,
      notes: '',
      items: [],
    },
  })

  const items = form.watch('items') ?? []
  const total = items.reduce((sum, item) => {
    const pItem = pricing?.items.find((p) => p.productId === item.productId)
    return sum + (item.quantity * (pItem?.dealerCasePrice ?? item.unitPrice))
  }, 0)

  function onSubmit(data: CreateSalesOrderInput) {
    mutate({
      ...data,
      distributorId: role === 'Admin' ? data.distributorId : null,
    })
  }

  return (
    <div className="flex flex-col gap-6 p-6">
      {/* Breadcrumb */}
      <div className="flex items-center gap-2 text-sm text-muted-foreground">
        <button onClick={() => router.push('/sales-orders')} className="hover:text-foreground">
          Sales Orders
        </button>
        <span>/</span>
        <span className="text-foreground font-medium">New Order</span>
      </div>

      <FormProvider {...form}>
        <form onSubmit={form.handleSubmit(onSubmit)} className="flex flex-row gap-6">
          {/* Left panel */}
          <div className="flex-1 flex flex-col gap-4">
            {/* Order Details card */}
            <Card>
              <CardHeader>
                <CardTitle className="text-base">Order Details</CardTitle>
              </CardHeader>
              <CardContent className="flex flex-col gap-4">
                {role === 'Admin' && (
                  <FormField
                    control={form.control}
                    name="distributorId"
                    render={({ field }) => (
                      <FormItem>
                        <FormLabel>Distributor</FormLabel>
                        <Select
                          value={field.value ? String(field.value) : ''}
                          onValueChange={(val) => field.onChange(val ? parseInt(val) : null)}
                          disabled={isLoadingDistributors}
                        >
                          <FormControl>
                            <SelectTrigger>
                              <SelectValue placeholder={isLoadingDistributors ? 'Loading...' : 'Select distributor'} />
                            </SelectTrigger>
                          </FormControl>
                          <SelectContent>
                            {distributors.map((d) => (
                              <SelectItem key={d.id} value={String(d.id)}>
                                {d.name}
                              </SelectItem>
                            ))}
                          </SelectContent>
                        </Select>
                        <FormMessage />
                        {fieldErrors?.distributorId && (
                          <p className="text-xs text-destructive">{fieldErrors.distributorId}</p>
                        )}
                      </FormItem>
                    )}
                  />
                )}
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

            {/* Line items */}
            <SalesOrderLineItemsForm
              pricingItems={pricing?.items ?? []}
              isLoadingPricing={isLoadingPricing}
              pricingError={pricingError}
            />
          </div>

          {/* Right summary sidebar */}
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
                  Save as Draft
                </Button>
                <Button
                  type="button"
                  variant="ghost"
                  className="w-full"
                  onClick={() => router.push('/sales-orders')}
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
