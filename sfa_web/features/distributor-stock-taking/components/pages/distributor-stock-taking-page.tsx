'use client'

import { useState, useEffect } from 'react'
import { useForm, useFieldArray, useWatch } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import {
  Plus, Trash2, Save, Send, Lock,
  CheckCircle2, PackageSearch,
} from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Badge } from '@/components/ui/badge'
import { Separator } from '@/components/ui/separator'
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
  FormMessage,
} from '@/components/ui/form'
import { AsyncSelect } from '@/components/async-select'
import {
  useOpenPeriods,
  useMySubmission,
  useUpsertDraft,
  useSubmitStockTaking,
} from '../../hooks/distributor-stock-taking.hooks'
import {
  upsertDraftSchema,
  type UpsertDraftInput,
  type ProductForSelect,
} from '../../schema/distributor-stock-taking.schema'
import { searchProductsForDistributorAction } from '../../actions/distributor-stock-taking.actions'

const MONTHS_FULL = [
  '', 'January', 'February', 'March', 'April', 'May', 'June',
  'July', 'August', 'September', 'October', 'November', 'December',
]

async function fetchProducts(search?: string): Promise<ProductForSelect[]> {
  if (!search || search.trim().length === 0) return []
  const result = await searchProductsForDistributorAction(search.trim())
  if (!result.success) return []
  return result.data
}

export function DistributorStockTakingPage() {
  const [selectedPeriodId, setSelectedPeriodId] = useState<number | null>(null)

  const { data: openPeriods = [], isLoading: isLoadingPeriods } = useOpenPeriods()
  const { data: existingSubmission, isLoading: isLoadingSubmission } =
    useMySubmission(selectedPeriodId)

  const selectedPeriod = openPeriods.find((p) => p.id === selectedPeriodId) ?? null
  const isLocked = selectedPeriod?.status === 'Locked'
  const isSubmitted = existingSubmission?.status === 'Submitted'

  const { mutate: saveDraft, isPending: isSaving } = useUpsertDraft()
  const { mutate: submitCount, isPending: isSubmitting } = useSubmitStockTaking()

  const form = useForm<UpsertDraftInput>({
    resolver: zodResolver(upsertDraftSchema),
    defaultValues: { periodId: 0, lines: [] },
  })

  const { fields, append, remove } = useFieldArray({ control: form.control, name: 'lines' })
  const watchedLines = useWatch({ control: form.control, name: 'lines' })

  useEffect(() => {
    if (existingSubmission) {
      form.reset({
        periodId: existingSubmission.stockTakingPeriodId,
        lines: existingSubmission.lines.map((l) => ({
          productId: l.productId,
          stockType: l.stockType as 'Normal' | 'FreeIssue',
          countedQuantity: l.countedQuantity,
        })),
      })
    } else if (selectedPeriodId) {
      form.reset({ periodId: selectedPeriodId, lines: [] })
    }
  }, [existingSubmission, selectedPeriodId, form])

  const handlePeriodChange = (value: string) => {
    const id = Number(value)
    setSelectedPeriodId(id)
    form.setValue('periodId', id)
  }

  const totalLines = fields.length
  const totalCounted = watchedLines.reduce((sum, l) => sum + (l.countedQuantity ?? 0), 0)
  const isPending = isSaving || isSubmitting
  const isReadOnly = isLocked || isSubmitted

  if (isLoadingPeriods) {
    return (
      <div className="flex flex-col gap-6 p-6">
        <div className="flex items-center gap-4 bg-muted/90 p-10 rounded-lg">
          <div>
            <h1 className="text-3xl font-bold tracking-tight">Stock Taking</h1>
            <p className="text-muted-foreground">Count your physical inventory</p>
          </div>
        </div>
        <div className="flex items-center justify-center py-24">
          <Spinner className="size-5" />
        </div>
      </div>
    )
  }

  return (
    <div className="flex flex-col gap-6 p-6">

      {/* ── Header ───────────────────────────────────────────────── */}
      <div className="flex items-center justify-between bg-muted/90 p-10 rounded-lg">
        <div className="flex items-center gap-4">
          <div>
            <h1 className="text-3xl font-bold tracking-tight">Stock Taking</h1>
            <p className="text-muted-foreground">
              Count your physical inventory and submit for reconciliation
            </p>
          </div>
        </div>
        {existingSubmission && (
          <Badge
            className={
              existingSubmission.status === 'Submitted'
                ? 'bg-emerald-100 text-emerald-700'
                : 'bg-muted text-muted-foreground'
            }
          >
            {existingSubmission.status}
          </Badge>
        )}
      </div>

      {/* ── Period selector ───────────────────────────────────────── */}
      <Card>
        <CardHeader className="pb-3">
          <CardTitle className="text-base">Counting Period</CardTitle>
        </CardHeader>
        <CardContent>
          {openPeriods.length === 0 ? (
            <p className="text-sm text-muted-foreground">
              No open stock taking periods available at this time.
            </p>
          ) : (
            <Select
              value={selectedPeriodId ? String(selectedPeriodId) : ''}
              onValueChange={handlePeriodChange}
            >
              <SelectTrigger className="w-64">
                <SelectValue placeholder="Select a period..." />
              </SelectTrigger>
              <SelectContent>
                {openPeriods.map((p) => (
                  <SelectItem key={p.id} value={String(p.id)}>
                    {MONTHS_FULL[p.month]} {p.year}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          )}
        </CardContent>
      </Card>

      {/* ── Content (only when period selected) ──────────────────── */}
      {selectedPeriodId && (
        <>
          {/* Status banners */}
          {isLocked && (
            <div className="flex items-center gap-3 rounded-lg border border-amber-200 bg-amber-50 px-4 py-3 text-amber-800">
              <Lock className="h-4 w-4 shrink-0" />
              <p className="text-sm font-medium">
                This period has been locked by the admin. No further changes can be made.
              </p>
            </div>
          )}
          {isSubmitted && !isLocked && (
            <div className="flex items-center gap-3 rounded-lg border border-emerald-200 bg-emerald-50 px-4 py-3 text-emerald-800">
              <CheckCircle2 className="h-4 w-4 shrink-0" />
              <p className="text-sm font-medium">
                Your stock count has been submitted. The admin will review and adjust the system inventory.
              </p>
            </div>
          )}

          {isLoadingSubmission ? (
            <div className="flex items-center justify-center py-16">
              <Spinner className="size-5" />
            </div>
          ) : (
            <Form {...form}>
              <form onSubmit={form.handleSubmit((data) => saveDraft(data))}>
                <div className="grid grid-cols-1 lg:grid-cols-[1fr_280px] gap-6 items-start">

                  {/* ── Left: product table ───────────────────── */}
                  <Card>
                    <CardHeader className="pb-3">
                      <div className="flex items-center justify-between">
                        <div>
                          <CardTitle className="text-base">Products to Count</CardTitle>
                          <p className="text-xs text-muted-foreground mt-0.5">
                            {fields.length} {fields.length === 1 ? 'product' : 'products'} in list
                          </p>
                        </div>
                        {!isReadOnly && (
                          <Button
                            type="button"
                            variant="outline"
                            size="sm"
                            className="gap-1.5"
                            onClick={() =>
                              append({ productId: 0, stockType: 'Normal', countedQuantity: 0 })
                            }
                          >
                            <Plus className="h-4 w-4" />
                            Add Product
                          </Button>
                        )}
                      </div>
                    </CardHeader>

                    <CardContent className="p-0">
                      {fields.length === 0 ? (
                        /* ── Empty state ── */
                        <div className="flex flex-col items-center justify-center gap-4 py-16 px-6 text-center">
                          <div className="flex h-16 w-16 items-center justify-center rounded-2xl bg-muted">
                            <PackageSearch className="h-8 w-8 text-muted-foreground" />
                          </div>
                          <div>
                            <p className="text-sm font-semibold">No products added yet</p>
                            <p className="text-xs text-muted-foreground mt-1">
                              Click &ldquo;Add Product&rdquo; to start entering your physical counts.
                            </p>
                          </div>
                          {!isReadOnly && (
                            <Button
                              type="button"
                              variant="outline"
                              className="gap-2 mt-1"
                              onClick={() =>
                                append({ productId: 0, stockType: 'Normal', countedQuantity: 0 })
                              }
                            >
                              <Plus className="h-4 w-4" />
                              Add First Product
                            </Button>
                          )}
                        </div>
                      ) : (
                        /* ── Table ── */
                        <>
                          <div className="overflow-x-auto">
                            <table className="w-full text-sm">
                              <thead>
                                <tr className="border-b bg-muted/30">
                                  <th className="py-2.5 px-4 text-left text-xs font-medium text-muted-foreground uppercase tracking-wide w-8">#</th>
                                  <th className="py-2.5 px-4 text-left text-xs font-medium text-muted-foreground uppercase tracking-wide">Product</th>
                                  <th className="py-2.5 px-4 text-left text-xs font-medium text-muted-foreground uppercase tracking-wide w-36">Stock Type</th>
                                  <th className="py-2.5 px-4 text-center text-xs font-medium text-muted-foreground uppercase tracking-wide w-36">Qty on Hand</th>
                                  {!isReadOnly && <th className="py-2.5 px-4 w-10" />}
                                </tr>
                              </thead>
                              <tbody className="divide-y">
                                {fields.map((field, index) => {
                                  const line = watchedLines[index]
                                  return (
                                    <tr key={field.id} className="hover:bg-muted/20 transition-colors">
                                      {/* # */}
                                      <td className="py-3 px-4 text-xs text-muted-foreground font-medium">
                                        {index + 1}
                                      </td>

                                      {/* Product */}
                                      <td className="py-3 px-4">
                                        {isReadOnly ? (
                                          <span className="text-sm font-medium">{line?.productId}</span>
                                        ) : (
                                          <FormField
                                            control={form.control}
                                            name={`lines.${index}.productId`}
                                            render={({ field: f }) => (
                                              <FormItem className="mb-0">
                                                <FormControl>
                                                  <AsyncSelect<ProductForSelect>
                                                    label="product"
                                                    placeholder="Search product..."
                                                    fetcher={fetchProducts}
                                                    value={f.value ? String(f.value) : ''}
                                                    onChange={(v) => f.onChange(v ? Number(v) : 0)}
                                                    getOptionValue={(p) => String(p.id)}
                                                    getDisplayValue={(p) => `${p.code} — ${p.itemDescription}`}
                                                    renderOption={(p) => (
                                                      <div>
                                                        <p className="font-semibold text-sm">{p.code}</p>
                                                        <p className="text-xs text-muted-foreground">{p.itemDescription}</p>
                                                      </div>
                                                    )}
                                                    clearable
                                                  />
                                                </FormControl>
                                                <FormMessage />
                                              </FormItem>
                                            )}
                                          />
                                        )}
                                      </td>

                                      {/* Stock type */}
                                      <td className="py-3 px-4">
                                        {isReadOnly ? (
                                          <Badge variant="outline" className="text-xs">
                                            {line?.stockType}
                                          </Badge>
                                        ) : (
                                          <FormField
                                            control={form.control}
                                            name={`lines.${index}.stockType`}
                                            render={({ field: f }) => (
                                              <FormItem className="mb-0">
                                                <Select value={f.value} onValueChange={f.onChange}>
                                                  <FormControl>
                                                    <SelectTrigger className="h-8 w-full text-xs">
                                                      <SelectValue />
                                                    </SelectTrigger>
                                                  </FormControl>
                                                  <SelectContent>
                                                    <SelectItem value="Normal">Normal</SelectItem>
                                                    <SelectItem value="FreeIssue">Free Issue</SelectItem>
                                                  </SelectContent>
                                                </Select>
                                                <FormMessage />
                                              </FormItem>
                                            )}
                                          />
                                        )}
                                      </td>

                                      {/* Quantity */}
                                      <td className="py-3 px-4 text-center">
                                        {isReadOnly ? (
                                          <span className="font-mono font-semibold">
                                            {line?.countedQuantity ?? 0}
                                          </span>
                                        ) : (
                                          <FormField
                                            control={form.control}
                                            name={`lines.${index}.countedQuantity`}
                                            render={({ field: f }) => (
                                              <FormItem className="mb-0">
                                                <FormControl>
                                                  <Input
                                                    type="number"
                                                    min={0}
                                                    step="0.0001"
                                                    className="h-8 w-24 mx-auto text-center font-mono font-semibold"
                                                    {...f}
                                                    onChange={(e) =>
                                                      f.onChange(parseFloat(e.target.value) || 0)
                                                    }
                                                  />
                                                </FormControl>
                                                <FormMessage />
                                              </FormItem>
                                            )}
                                          />
                                        )}
                                      </td>

                                      {/* Remove */}
                                      {!isReadOnly && (
                                        <td className="py-3 px-4 text-center">
                                          <Button
                                            type="button"
                                            variant="ghost"
                                            size="icon"
                                            className="h-7 w-7 text-muted-foreground hover:text-destructive hover:bg-destructive/10"
                                            onClick={() => remove(index)}
                                          >
                                            <Trash2 className="h-3.5 w-3.5" />
                                          </Button>
                                        </td>
                                      )}
                                    </tr>
                                  )
                                })}
                              </tbody>
                            </table>
                          </div>

                          {/* Add another — inline link */}
                          {!isReadOnly && (
                            <div className="px-4 py-3 border-t">
                              <Button
                                type="button"
                                variant="ghost"
                                size="sm"
                                className="h-auto p-0 text-sm text-primary font-medium hover:bg-transparent hover:underline gap-1"
                                onClick={() =>
                                  append({ productId: 0, stockType: 'Normal', countedQuantity: 0 })
                                }
                              >
                                <Plus className="h-3.5 w-3.5" />
                                Add another product
                              </Button>
                            </div>
                          )}
                        </>
                      )}
                    </CardContent>
                  </Card>

                  {/* ── Right: summary + actions ──────────────── */}
                  <div className="space-y-4 lg:sticky lg:top-6">

                    {/* Summary */}
                    <Card>
                      <CardHeader className="pb-2">
                        <CardTitle className="text-base">Summary</CardTitle>
                      </CardHeader>
                      <CardContent className="space-y-3 text-sm">
                        <div className="flex justify-between items-center">
                          <span className="text-muted-foreground">Period</span>
                          <span className="font-semibold text-right">
                            {selectedPeriod
                              ? `${MONTHS_FULL[selectedPeriod.month]} ${selectedPeriod.year}`
                              : '—'}
                          </span>
                        </div>
                        <div className="flex justify-between items-center">
                          <span className="text-muted-foreground">Products</span>
                          <span className="font-semibold">{totalLines}</span>
                        </div>
                        <div className="flex justify-between items-center">
                          <span className="text-muted-foreground">Total Qty</span>
                          <span className="font-semibold font-mono">{totalCounted.toFixed(2)}</span>
                        </div>
                        <div className="flex justify-between items-center">
                          <span className="text-muted-foreground">Status</span>
                          {existingSubmission ? (
                            <Badge
                              className={
                                existingSubmission.status === 'Submitted'
                                  ? 'bg-emerald-100 text-emerald-700'
                                  : 'bg-muted text-muted-foreground'
                              }
                            >
                              {existingSubmission.status}
                            </Badge>
                          ) : (
                            <span className="text-muted-foreground text-xs">Not started</span>
                          )}
                        </div>
                        <Separator />
                        <p className="text-xs text-muted-foreground leading-relaxed">
                          Enter the physical quantity you see in your warehouse.
                          System stock is recorded at submission time.
                        </p>
                      </CardContent>
                    </Card>

                    {/* Actions */}
                    {!isReadOnly && (
                      <>
                        <Button
                          type="button"
                          className="w-full gap-2"
                          disabled={isPending || fields.length === 0}
                          onClick={() => {
                            if (!selectedPeriodId) return
                            submitCount(selectedPeriodId)
                          }}
                        >
                          {isSubmitting ? <Spinner className="h-4 w-4" /> : <Send className="h-4 w-4" />}
                          Submit Count
                        </Button>
                        <Button
                          type="submit"
                          variant="outline"
                          className="w-full gap-2"
                          disabled={isPending || fields.length === 0}
                        >
                          {isSaving ? <Spinner className="h-4 w-4" /> : <Save className="h-4 w-4" />}
                          Save Draft
                        </Button>
                      </>
                    )}
                  </div>
                </div>
              </form>
            </Form>
          )}
        </>
      )}
    </div>
  )
}
