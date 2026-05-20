'use client'

import { useCallback } from 'react'
import Link from 'next/link'
import { useRouter } from 'next/navigation'
import { useForm, useFieldArray, useWatch } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { FileText, Minus, Plus, ShoppingCart, Trash2, Save, Send } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Separator } from '@/components/ui/separator'
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
  useMyDistributorProfile,
  useMyProductCategoryPricings,
  useCreateMyPurchaseOrder,
  useSubmitMyPurchaseOrder,
} from '../../hooks/distributor-purchase-order.hooks'
import {
  createMyPurchaseOrderSchema,
  type CreateMyPurchaseOrderInput,
} from '../../schema/distributor-purchase-order.schema'

function formatCurrency(amount: number) {
  return new Intl.NumberFormat('en-LK', {
    style: 'currency',
    currency: 'LKR',
    minimumFractionDigits: 2,
  }).format(amount)
}

// ── Workflow step indicator ────────────────────────────────────────────────

type StepVariant = 'approval' | 'rejection'

const workflowSteps: { label: string; description: string; variant?: StepVariant }[] = [
  { label: 'Draft', description: 'You are here — fill in the details' },
  { label: 'Pending Rep Approval', description: 'Sales Rep reviews & approves' },
  { label: 'Pending Manager Approval', description: 'Manager gives final sign-off' },
  { label: 'Pending Finalization', description: 'You confirm & finalise the order' },
  { label: 'Finalized', description: 'Order confirmed & dispatched', variant: 'approval' },
  { label: 'Pending Acknowledgement', description: 'Rejected — you must acknowledge', variant: 'rejection' },
  { label: 'Cancelled', description: 'Rejection acknowledged', variant: 'rejection' },
]

function WorkflowStepper() {
  return (
    <div className="space-y-0">
      {workflowSteps.map((step, i) => {
        const isActive = i === 0
        const isApproval = step.variant === 'approval'
        const isRejection = step.variant === 'rejection'

        const dotClass = isActive
          ? 'border-primary bg-primary text-primary-foreground'
          : isApproval
            ? 'border-emerald-500 bg-emerald-50 text-emerald-600'
            : isRejection
              ? 'border-destructive/50 bg-destructive/5 text-destructive'
              : 'border-muted-foreground/30 bg-background text-muted-foreground'

        return (
          <div key={step.label} className="flex gap-3">
            <div className="flex flex-col items-center">
              <div className={`flex h-7 w-7 shrink-0 items-center justify-center rounded-full border-2 text-xs font-bold ${dotClass}`}>
                {isActive ? <span className="text-[10px]">✓</span> : <span>{i + 1}</span>}
              </div>
              {i < workflowSteps.length - 1 && (
                <div className={`my-1 h-8 w-px ${isRejection ? 'bg-destructive/20' : 'bg-muted-foreground/20'}`} />
              )}
            </div>
            <div className="pb-4 pt-0.5">
              <p className={`text-sm font-medium ${isRejection ? 'text-destructive/80' : i > 0 ? 'text-muted-foreground' : ''}`}>
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

export function DistributorPurchaseOrderCreatePage() {
  const router = useRouter()

  const { data: categoryPricings = [], isLoading: isLoadingPricings } = useMyProductCategoryPricings()
  const products = categoryPricings.map((r) => ({ id: r.productId, itemDescription: r.itemDescription }))
  const { data: profile, isLoading: isLoadingProfile } = useMyDistributorProfile()

  const { mutate: createOrder, isPending: isCreating, fieldErrors } = useCreateMyPurchaseOrder()
  const { mutate: submitOrder, isPending: isSubmitting } = useSubmitMyPurchaseOrder(
    (id) => router.push(`/distributor-purchase-orders/${id}`)
  )

  const form = useForm<CreateMyPurchaseOrderInput>({
    resolver: zodResolver(createMyPurchaseOrderSchema),
    defaultValues: {
      notes: '',
      items: [{ productId: 0, quantity: 1, unitPrice: 0, discount: 0 }],
    },
  })

  const { fields, append, remove } = useFieldArray({
    control: form.control,
    name: 'items',
  })

  const watchedItems = useWatch({ control: form.control, name: 'items' })

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
    form.setValue(`items.${index}.unitPrice`, getCategoryPrice(productId))
  }, [form, getCategoryPrice])

  const subtotal = watchedItems.reduce((sum, item) => {
    const line = (item.unitPrice ?? 0) * (item.quantity ?? 0)
    return sum + line * (1 - (item.discount ?? 0) / 100)
  }, 0)

  const totalCases = watchedItems.reduce((sum, item) => sum + (item.quantity ?? 0), 0)

  const hasValidItem = watchedItems.some((i) => (i.productId ?? 0) > 0)
  const hasUnpricedItems = watchedItems.some(
    (i) => (i.productId ?? 0) > 0 && getCategoryPrice(i.productId) === 0
  )
  const canSubmit = hasValidItem && !hasUnpricedItems

  const onSubmitForApproval = (data: CreateMyPurchaseOrderInput) => {
    const validItems = data.items.filter((i) => i.productId > 0)
    createOrder(
      { ...data, items: validItems },
      { onSuccess: (created) => submitOrder(created.id) }
    )
  }

  const onSaveDraft = () => {
    const data = form.getValues()
    const validItems = data.items.filter((i) => i.productId > 0)
    if (validItems.length === 0) return
    createOrder(
      { ...data, items: validItems },
      { onSuccess: (created) => router.push(`/distributor-purchase-orders/${created.id}`) }
    )
  }

  const isLoading = isLoadingPricings || isLoadingProfile
  const isPending = isCreating || isSubmitting

  if (isLoading) {
    return (
      <div className="flex flex-col gap-6 p-6 mx-auto min-w-3/4">
        <div className="flex items-center justify-between bg-muted/90 p-10 rounded-lg">
          <div>
            <h1 className="text-3xl font-bold tracking-tight">Create Purchase Order</h1>
            <p className="text-muted-foreground">Add products, set quantities, and submit for approval.</p>
          </div>
        </div>
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
          <h1 className="text-3xl font-bold tracking-tight">Create Purchase Order</h1>
          <p className="text-muted-foreground">Add products, set quantities, and submit for approval.</p>
        </div>
      </div>

      <Form {...form}>
        <form onSubmit={form.handleSubmit(onSubmitForApproval)}>
          <div className="grid grid-cols-1 lg:grid-cols-[1fr_300px] gap-6 items-start">

            {/* ── Left column ─────────────────────────────────────── */}
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
                      <p className="text-xs text-muted-foreground">Customer and distribution info</p>
                    </div>
                  </div>
                </CardHeader>
                <CardContent className="space-y-4">
                  {/* Distributor info — read-only for portal */}
                  <div className="rounded-md border bg-muted/30 px-4 py-3">
                    <p className="text-xs text-muted-foreground mb-0.5">Distributor</p>
                    <p className="font-semibold">{profile?.name ?? '—'}</p>
                    <p className="text-xs text-muted-foreground mt-0.5">
                      Category <span className="font-medium text-foreground">{profile?.category ?? '—'}</span> pricing
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
                            placeholder="Any instructions or remarks for this order..."
                            rows={3}
                            maxLength={1000}
                            className="resize-none"
                          />
                        </FormControl>
                        <FormMessage />
                      </FormItem>
                    )}
                  />
                </CardContent>
              </Card>

              {/* Order Items */}
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
                          {fields.length} product{fields.length !== 1 ? 's' : ''} · prices from{' '}
                          {profile?.category ? `category ${profile.category} pricing` : 'category pricing'}
                        </p>
                      </div>
                    </div>
                    <Button
                      type="button"
                      variant="outline"
                      size="sm"
                      onClick={() => append({ productId: 0, quantity: 1, unitPrice: 0, discount: 0 })}
                    >
                      <Plus className="h-4 w-4 mr-1" />
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
                        <tr className="border-b bg-muted/40">
                          <th className="pb-2 pt-2 px-2 text-left text-xs font-medium text-muted-foreground uppercase tracking-wide w-6">#</th>
                          <th className="pb-2 pt-2 px-2 text-left text-xs font-medium text-muted-foreground uppercase tracking-wide">Product</th>
                          <th className="pb-2 pt-2 px-2 text-center text-xs font-medium text-muted-foreground uppercase tracking-wide w-36">QTY</th>
                          <th className="pb-2 pt-2 px-2 text-right text-xs font-medium text-muted-foreground uppercase tracking-wide w-36">Case Price</th>
                          <th className="pb-2 pt-2 px-2 text-right text-xs font-medium text-muted-foreground uppercase tracking-wide w-28">Line Total</th>
                          <th className="pb-2 pt-2 px-2 w-8" />
                        </tr>
                      </thead>
                      <tbody className="divide-y">
                        {fields.map((field, index) => {
                          const watchedItem = watchedItems[index]
                          const pid = watchedItem?.productId ?? 0
                          const price = pid ? getCategoryPrice(pid) : null
                          const lineTotal = (watchedItem?.unitPrice ?? 0) * (watchedItem?.quantity ?? 0) * (1 - (watchedItem?.discount ?? 0) / 100)

                          return (
                            <tr key={field.id}>
                              <td className="py-2.5 px-2 text-muted-foreground text-xs">{index + 1}</td>
                              <td className="py-2.5 px-2">
                                <Select
                                  value={watchedItem?.productId ? String(watchedItem.productId) : ''}
                                  onValueChange={(v) => handleProductChange(index, Number(v))}
                                >
                                  <SelectTrigger className="h-8 w-full min-w-[180px]">
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
                              <td className="py-2.5 px-2">
                                <div className="flex items-center justify-center gap-1">
                                  <Button
                                    type="button"
                                    variant="outline"
                                    size="icon"
                                    className="h-7 w-7 shrink-0"
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
                                    className="h-7 w-7 shrink-0"
                                    onClick={() => {
                                      const cur = form.getValues(`items.${index}.quantity`)
                                      form.setValue(`items.${index}.quantity`, cur + 1)
                                    }}
                                  >
                                    <Plus className="h-3 w-3" />
                                  </Button>
                                </div>
                              </td>
                              <td className="py-2.5 px-2 text-right">
                                {!pid ? (
                                  <span className="text-sm text-muted-foreground">—</span>
                                ) : price === 0 ? (
                                  <span className="text-xs text-destructive font-medium">No price</span>
                                ) : (
                                  <div className="flex flex-col items-end gap-0.5">
                                    <span className="text-sm font-medium tabular-nums">{formatCurrency(price ?? 0)}</span>
                                    <span className="text-[10px] text-muted-foreground leading-none">Cat {profile?.category}</span>
                                  </div>
                                )}
                              </td>
                              <td className="py-2.5 px-2 text-right tabular-nums font-medium">
                                {formatCurrency(lineTotal)}
                              </td>
                              <td className="py-2.5 px-2">
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

                  <div className="mt-3">
                    <Button
                      type="button"
                      variant="ghost"
                      size="sm"
                      className="text-primary h-auto p-0 text-sm font-medium hover:bg-transparent hover:underline"
                      onClick={() => append({ productId: 0, quantity: 1, unitPrice: 0, discount: 0 })}
                    >
                      <Plus className="h-3.5 w-3.5 mr-1" />
                      Add another product
                    </Button>
                  </div>

                  <div className="mt-4 border-t pt-4 space-y-2">
                    <div className="flex justify-between text-sm">
                      <span className="text-muted-foreground">Subtotal</span>
                      <span className="tabular-nums">{formatCurrency(subtotal)}</span>
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
                      <span className="tabular-nums text-primary">{formatCurrency(subtotal)}</span>
                    </div>
                  </div>
                </CardContent>
              </Card>
            </div>

            {/* ── Right sidebar ────────────────────────────────────── */}
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
                    <span className="font-medium text-muted-foreground">Draft</span>
                  </div>
                  <Separator />
                  <p className="text-xs text-muted-foreground">
                    Prices are auto-filled from category pricing and cannot be edited.
                  </p>
                  {hasUnpricedItems && (
                    <p className="text-xs text-destructive font-medium">
                      Some products have no price set for category {profile?.category}. Submission is
                      disabled until all items have a price.
                    </p>
                  )}
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
                {isPending ? <Spinner className="mr-2" /> : <Send className="h-4 w-4 mr-2" />}
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
                <Link href="/distributor-purchase-orders">Discard &amp; Cancel</Link>
              </Button>
            </div>
          </div>
        </form>
      </Form>
    </div>
  )
}
