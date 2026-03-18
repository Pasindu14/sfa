'use client'

import { useCallback, useState } from 'react'
import Link from 'next/link'
import { useRouter } from 'next/navigation'
import { useForm, useFieldArray, useWatch, Controller } from "react-hook-form"
import { format } from 'date-fns'
import { zodResolver } from '@hookform/resolvers/zod'
import {
  ShoppingCart,
  FileText,
  Minus,
  Plus,
  Trash2,
  Send,
  Save,
  CalendarIcon,
} from "lucide-react"
import { Calendar } from '@/components/ui/calendar'
import { Popover, PopoverContent, PopoverTrigger } from '@/components/ui/popover'
import { useSession } from 'next-auth/react'
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
import { useDefaultPricingStructure, useCreatePurchaseOrder, useSubmitPurchaseOrder } from '../../hooks/purchase-order.hooks'
import {
  createPurchaseOrderSchema,
  type CreatePurchaseOrderInput,
} from '../../schema/purchase-order.schema'
import { formatCurrency } from '../../utils/format'

// ── Distributor query ─────────────────────────────────────────────────────

function useDistributors() {
  return useQuery({
    queryKey: ['distributors', 'list', { pageSize: 1000, activeOnly: true }],
    queryFn: async () => {
      const result = await getDistributorsAction(1, 1000)
      if (!result.success) throw new Error(result.error)
      return result.data.distributors.filter((d) => d.isActive)
    },
    staleTime: 5 * 60 * 1000,
  })
}

// ── Workflow step indicator ────────────────────────────────────────────────

type StepVariant = 'approval' | 'rejection'

const workflowSteps: { label: string; description: string; variant?: StepVariant }[] = [
  { label: 'Draft', description: 'You are here — fill in the details' },
  { label: 'Pending Rep Approval', description: 'Sales Rep reviews & approves' },
  { label: 'Pending Manager Approval', description: 'Sales Manager gives final sign-off' },
  { label: 'Pending Finalization', description: 'Distributor confirms & finalises' },
  { label: 'Finalized', description: 'Order confirmed & dispatched', variant: 'approval' },
  { label: 'Pending Acknowledgement', description: 'Rep/Manager rejected — Distributor must acknowledge', variant: 'rejection' },
  { label: 'Cancelled', description: 'Distributor acknowledged the rejection', variant: 'rejection' },
]

function WorkflowStepper() {
  return (
    <div className="space-y-0">
      {workflowSteps.map((step, i) => {
        const isActive = i === 0
        const isApproval = step.variant === 'approval'
        const isRejection = step.variant === 'rejection'
        const isUpcoming = i > 0

        const dotClass = isActive
          ? 'border-primary bg-primary text-primary-foreground'
          : isApproval
            ? 'border-emerald-500 bg-emerald-50 text-emerald-600'
            : isRejection
              ? 'border-destructive/50 bg-destructive/5 text-destructive'
              : 'border-muted-foreground/30 bg-background text-muted-foreground'

        return (
          <div key={step.label} className="flex gap-3">
            {/* Icon column */}
            <div className="flex flex-col items-center">
              <div className={`flex h-7 w-7 shrink-0 items-center justify-center rounded-full border-2 text-xs font-bold ${dotClass}`}>
                {isActive ? (
                  <span className="text-[10px]">✓</span>
                ) : (
                  <span>{i + 1}</span>
                )}
              </div>
              {i < workflowSteps.length - 1 && (
                <div className={`my-1 h-8 w-px ${isRejection ? 'bg-destructive/20' : 'bg-muted-foreground/20'}`} />
              )}
            </div>
            {/* Text column */}
            <div className="pb-4 pt-0.5">
              <p className={`text-sm font-medium ${isRejection ? 'text-destructive/80' : isUpcoming ? 'text-muted-foreground' : ''}`}>
                {step.label}
              </p>
              <p className="text-xs text-muted-foreground">{step.description}</p>
            </div>
          </div>
        )
      })}
    </div>
  )
}

// ── Create Page ────────────────────────────────────────────────────────────

export function PurchaseOrderCreatePage() {
  const [orderDate, setOrderDate] = useState<Date>(new Date())
  const { data: session } = useSession()
  const isAdmin = session?.user?.role === 'Admin'

  const { data: products, isLoading: isLoadingProducts } = useAllActiveProducts()
  const { data: pricing, isLoading: isLoadingPricing } = useDefaultPricingStructure()
  const { data: distributors, isLoading: isLoadingDistributors } = useDistributors()
  const router = useRouter()
  const { mutate: createOrder, isPending, fieldErrors } = useCreatePurchaseOrder()
  const { mutate: submitOrder } = useSubmitPurchaseOrder()

  const form = useForm<CreatePurchaseOrderInput>({
    resolver: zodResolver(createPurchaseOrderSchema),
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

  const watchedItems = useWatch({ control: form.control, name: "items" });

  const subtotal = watchedItems.reduce((sum, item) => {
    const line = (item.unitPrice ?? 0) * (item.quantity ?? 0)
    return sum + line * (1 - (item.discount ?? 0) / 100)
  }, 0)

  const totalCases = watchedItems.reduce(
    (sum, item) => sum + (item.quantity ?? 0),
    0,
  );

  const watchedDistributorId = useWatch({ control: form.control, name: 'distributorId' })

  const hasValidItem = watchedItems.some((i) => (i.productId ?? 0) > 0)
  const hasDistributor = !isAdmin || !!watchedDistributorId
  const canSubmit = hasValidItem && hasDistributor

  const onSubmitForApproval = (data: CreatePurchaseOrderInput) => {
    createOrder(
      { ...data, items: data.items.filter((i) => i.productId > 0) },
      {
        onSuccess: (created) =>
          submitOrder(created.id, {
            onSuccess: () => router.push(`/purchase-orders/${created.id}`),
          }),
      },
    )
  }

  const onSaveDraft = () => {
    const data = form.getValues()
    const validItems = data.items.filter((i) => i.productId > 0)
    if (validItems.length === 0) return
    createOrder(
      { ...data, items: validItems },
      { onSuccess: (created) => router.push(`/purchase-orders/${created.id}`) },
    )
  }

  if (isLoading) {
    return (
      <div className="flex flex-col gap-6 p-6 mx-auto min-w-3/4">
        {/* Header stays visible while loading */}
        <div className="flex items-center justify-between bg-muted/90 p-10 rounded-lg">
          <div>
            <h1 className="text-3xl font-bold tracking-tight">Create Purchase Order</h1>
            <p className="text-muted-foreground">Add products, set quantities, and submit for approval.</p>
          </div>
        </div>
        {/* Spinner in the body area */}
        <div className="flex items-center justify-center py-24">
          <Spinner className="size-4" />
        </div>
      </div>
    )
  }

  return (
    <div className="flex flex-col gap-6 p-6 mx-auto min-w-3/4">
      {/* Header */}
      <div className="flex items-center justify-between bg-muted/90 p-10 rounded-lg">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">
            Create Purchase Order
          </h1>
          <p className="text-muted-foreground">
            Add products, set quantities, and submit for approval.
          </p>
        </div>
      </div>

      {/* Form */}
      <Form {...form}>
        <form onSubmit={form.handleSubmit(onSubmitForApproval)}>
          <div className="grid grid-cols-1 lg:grid-cols-[1fr_300px] gap-6 items-start">
            {/* Left column */}
            <div className="space-y-6">
              {/* Order Details */}
              <Card>
                <CardHeader className="pb-3">
                  <div className="flex items-center gap-2">
                    <div className="flex h-8 w-8 items-center justify-center rounded-md bg-orange-100 text-orange-600">
                      <FileText className="h-4 w-4" />
                    </div>
                    <div>
                      <CardTitle className="text-base">Order Details</CardTitle>
                      <p className="text-xs text-muted-foreground">
                        Customer and distribution info
                      </p>
                    </div>
                  </div>
                </CardHeader>
                <CardContent className="space-y-4">
                  <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                    {/* Distributor */}
                    <FormField
                      control={form.control}
                      name="distributorId"
                      render={({ field }) => (
                        <FormItem>
                          <FormLabel>
                            Distributor{" "}
                            {isAdmin ? (
                              <span className="text-destructive text-xs font-semibold">*required</span>
                            ) : (
                              <span className="text-muted-foreground text-xs font-normal">(optional)</span>
                            )}
                          </FormLabel>
                          <Select
                            value={field.value ? String(field.value) : ""}
                            onValueChange={(v) =>
                              field.onChange(v ? Number(v) : null)
                            }
                          >
                            <FormControl>
                              <SelectTrigger className="w-full">
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
                            <p className="text-sm text-destructive">
                              {fieldErrors.distributorId}
                            </p>
                          )}
                        </FormItem>
                      )}
                    />

                    {/* Order Date */}
                    <FormItem>
                      <FormLabel>Order Date</FormLabel>
                      <Popover>
                        <PopoverTrigger asChild>
                          <Button
                            variant="outline"
                            className="w-full justify-start text-left font-normal"
                          >
                            <CalendarIcon className="mr-2 h-4 w-4 text-muted-foreground" />
                            {format(orderDate, 'PPP')}
                          </Button>
                        </PopoverTrigger>
                        <PopoverContent className="w-auto p-0">
                          <Calendar
                            mode="single"
                            selected={orderDate}
                            onSelect={(d) => d && setOrderDate(d)}
                          />
                        </PopoverContent>
                      </Popover>
                    </FormItem>
                  </div>

                  {/* Notes */}
                  <FormField
                    control={form.control}
                    name="notes"
                    render={({ field }) => (
                      <FormItem>
                        <FormLabel>
                          Notes{" "}
                          <span className="text-muted-foreground text-xs font-normal">
                            (optional)
                          </span>
                        </FormLabel>
                        <FormControl>
                          <Textarea
                            {...field}
                            placeholder="Any instructions or remarks for this order..."
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
                <CardHeader className="pb-3">
                  <div className="flex items-center justify-between">
                    <div className="flex items-center gap-2">
                      <div className="flex h-8 w-8 items-center justify-center rounded-md bg-orange-100 text-orange-600">
                        <ShoppingCart className="h-4 w-4" />
                      </div>
                      <div>
                        <CardTitle className="text-base">Order Items</CardTitle>
                        <p className="text-xs text-muted-foreground">
                          {fields.length} product
                          {fields.length !== 1 ? "s" : ""} · prices from default
                          price list
                        </p>
                      </div>
                    </div>
                    <Button
                      type="button"
                      variant="outline"
                      size="sm"
                      onClick={() =>
                        append({
                          productId: 0,
                          quantity: 1,
                          unitPrice: 0,
                          discount: 0,
                        })
                      }
                    >
                      <Plus className="h-4 w-4 mr-1" />
                      Add Product
                    </Button>
                  </div>
                </CardHeader>
                <CardContent className="p-0">
                  <div className="overflow-x-auto [&::-webkit-scrollbar]:h-2 [&::-webkit-scrollbar-track]:bg-muted/40 [&::-webkit-scrollbar-thumb]:rounded-full [&::-webkit-scrollbar-thumb]:bg-muted-foreground/30">
                  <div className="min-w-[640px]">
                  {/* Column headers */}
                  <div className="grid grid-cols-[32px_1fr_160px_140px_100px_32px] gap-3 px-6 py-2 border-b bg-muted/40">
                    <span className="text-xs font-medium text-muted-foreground uppercase tracking-wide">
                      #
                    </span>
                    <span className="text-xs font-medium text-muted-foreground uppercase tracking-wide">
                      Product
                    </span>
                    <span className="text-xs font-medium text-muted-foreground uppercase tracking-wide text-center">
                      QTY (Cases)
                    </span>
                    <span className="text-xs font-medium text-muted-foreground uppercase tracking-wide text-right">
                      Case Price
                    </span>
                    <span className="text-xs font-medium text-muted-foreground uppercase tracking-wide text-right">
                      Line Total
                    </span>
                    <span />
                  </div>

                  <div className="divide-y">
                    {fields.map((field, index) => {
                      const watchedItem = watchedItems[index];
                      const lineTotal =
                        (watchedItem?.unitPrice ?? 0) *
                        (watchedItem?.quantity ?? 0) *
                        (1 - (watchedItem?.discount ?? 0) / 100);

                      return (
                        <div
                          key={field.id}
                          className="grid grid-cols-[32px_1fr_160px_140px_100px_32px] gap-3 px-6 py-3 items-center"
                        >
                          {/* Row number */}
                          <span className="text-xs text-muted-foreground font-medium text-center">
                            {index + 1}
                          </span>

                          {/* Product select */}
                          <Controller
                            control={form.control}
                            name={`items.${index}.productId`}
                            render={({ field: f }) => (
                              <Select
                                value={f.value ? String(f.value) : ""}
                                onValueChange={(v) =>
                                  handleProductChange(index, Number(v))
                                }
                              >
                                <SelectTrigger className="h-8 text-sm md:w-full">
                                  <SelectValue placeholder="Select product..." />
                                </SelectTrigger>
                                <SelectContent>
                                  {products?.map((p) => (
                                    <SelectItem key={p.id} value={String(p.id)}>
                                      <span className="font-medium">
                                        {p.itemDescription}
                                      </span>
                                      <span className="ml-2 text-xs text-muted-foreground font-mono">
                                        {p.code}
                                      </span>
                                    </SelectItem>
                                  ))}
                                </SelectContent>
                              </Select>
                            )}
                          />

                          {/* Quantity stepper */}
                          <Controller
                            control={form.control}
                            name={`items.${index}.quantity`}
                            render={({ field: f }) => (
                              <div className="flex items-center gap-1">
                                <Button
                                  type="button"
                                  variant="outline"
                                  size="icon"
                                  className="h-8 w-8 shrink-0"
                                  onClick={() =>
                                    f.onChange(Math.max(1, (f.value ?? 1) - 1))
                                  }
                                >
                                  <Minus className="h-3 w-3" />
                                </Button>
                                <Input
                                  type="number"
                                  min={1}
                                  className="h-8 text-sm text-center w-14"
                                  value={f.value}
                                  onChange={(e) =>
                                    f.onChange(Number(e.target.value))
                                  }
                                />
                                <Button
                                  type="button"
                                  variant="outline"
                                  size="icon"
                                  className="h-8 w-8 shrink-0"
                                  onClick={() => f.onChange((f.value ?? 1) + 1)}
                                >
                                  <Plus className="h-3 w-3" />
                                </Button>
                              </div>
                            )}
                          />

                          {/* Unit Price — read-only, auto-filled from pricing structure */}
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
                            className="h-8 w-8 text-muted-foreground hover:text-destructive"
                            onClick={() => remove(index)}
                            disabled={fields.length === 1}
                          >
                            <Trash2 className="h-4 w-4" />
                          </Button>
                        </div>
                      );
                    })}
                  </div>
                  </div>{/* end min-w */}
                  </div>{/* end overflow-x-auto */}

                  {/* Add another link */}
                  <div className="px-6 py-3 border-t">
                    <Button
                      type="button"
                      variant="ghost"
                      size="sm"
                      className="text-primary h-auto p-0 text-sm font-medium hover:bg-transparent hover:underline"
                      onClick={() =>
                        append({
                          productId: 0,
                          quantity: 1,
                          unitPrice: 0,
                          discount: 0,
                        })
                      }
                    >
                      <Plus className="h-3.5 w-3.5 mr-1" />
                      Add another product
                    </Button>
                  </div>

                  {/* Totals */}
                  <div className="border-t px-6 py-4 space-y-2">
                    <div className="flex justify-between text-sm">
                      <span className="text-muted-foreground">Subtotal</span>
                      <span className="tabular-nums">
                        {formatCurrency(subtotal)}
                      </span>
                    </div>
                    <div className="flex justify-between text-sm">
                      <span className="text-muted-foreground">Discount</span>
                      <span className="text-muted-foreground">—</span>
                    </div>
                    <div className="flex justify-between text-sm">
                      <span className="text-muted-foreground">Tax (0%)</span>
                      <span className="tabular-nums">{formatCurrency(0)}</span>
                    </div>
                    <Separator />
                    <div className="flex justify-between font-bold text-base">
                      <span>Total Payable</span>
                      <span className="tabular-nums text-primary">
                        {formatCurrency(subtotal)}
                      </span>
                    </div>
                  </div>
                </CardContent>
              </Card>
            </div>

            {/* Right sidebar */}
            <div className="space-y-4 lg:sticky lg:top-6">
              {/* Order Summary */}
              <Card>
                <CardHeader className="pb-2">
                  <CardTitle className="text-base">Order Summary</CardTitle>
                </CardHeader>
                <CardContent className="space-y-3 text-sm">
                  <div className="flex justify-between">
                    <span className="text-muted-foreground">Line Items</span>
                    <span className="font-semibold">{fields.length}</span>
                  </div>
                  <div className="flex justify-between">
                    <span className="text-muted-foreground">Total Cases</span>
                    <span className="font-semibold">{totalCases}</span>
                  </div>
                  <div className="flex justify-between">
                    <span className="text-muted-foreground">Subtotal</span>
                    <span className="font-semibold tabular-nums text-primary">
                      {formatCurrency(subtotal)}
                    </span>
                  </div>
                  <div className="flex justify-between">
                    <span className="text-muted-foreground">Status</span>
                    <span className="font-medium text-muted-foreground">
                      Draft
                    </span>
                  </div>
                  <Separator />
                  <p className="text-xs text-muted-foreground">
                    Prices are auto-filled from the default price list and cannot be edited.
                  </p>
                </CardContent>
              </Card>

              {/* Approval Workflow */}
              <Card>
                <CardHeader className="pb-3">
                  <CardTitle className="text-base flex items-center gap-2">
                    <svg
                      className="h-4 w-4 text-muted-foreground"
                      fill="none"
                      viewBox="0 0 24 24"
                      stroke="currentColor"
                      strokeWidth={2}
                    >
                      <path
                        strokeLinecap="round"
                        strokeLinejoin="round"
                        d="M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z"
                      />
                    </svg>
                    Approval Workflow
                  </CardTitle>
                </CardHeader>
                <CardContent className="pt-0">
                  <WorkflowStepper />
                </CardContent>
              </Card>

              {/* Actions */}
              <Button
                type="submit"
                className="w-full bg-primary hover:bg-primary/90"
                disabled={isPending || !canSubmit}
              >
                {isPending ? (
                  <Spinner className="mr-2" />
                ) : (
                  <Send className="h-4 w-4 mr-2" />
                )}
                Submit for Approval
              </Button>

              <Button
                type="button"
                variant="outline"
                className="w-full"
                disabled={isPending || !hasValidItem}
                onClick={onSaveDraft}
              >
                <Save className="h-4 w-4 mr-2" />
                Save as Draft
              </Button>

              <Button
                type="button"
                variant="ghost"
                className="w-full text-muted-foreground"
                asChild
              >
                <Link href="/purchase-orders">Discard &amp; Cancel</Link>
              </Button>
            </div>
          </div>
        </form>
      </Form>
    </div>
  );
}
