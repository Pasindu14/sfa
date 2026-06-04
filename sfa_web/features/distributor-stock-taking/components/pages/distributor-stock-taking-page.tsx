'use client'

import { useState, useEffect } from 'react'
import { useForm, useFieldArray, useWatch } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import {
  Plus, Trash2, Send, Lock,
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
  useUpsertAndSubmit,
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
  const result = await searchProductsForDistributorAction(search?.trim() ?? '')
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

  const { mutate: submitCount, isPending: isSubmitting } = useUpsertAndSubmit()

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
  const isPending = isSubmitting
  const isReadOnly = isLocked || isSubmitted

  if (isLoadingPeriods) {
    return (
      <div className="flex flex-col gap-6 p-6 max-w-[75vw] mx-auto w-full">
        <div className="flex items-center gap-4 bg-muted/90 p-10 rounded-lg">
          <div>
            <h1 className="text-3xl font-bold tracking-tight">Stock Taking</h1>
            <p className="text-muted-foreground">
              Count your physical inventory
            </p>
          </div>
        </div>
        <div className="flex items-center justify-center py-24">
          <Spinner className="size-5" />
        </div>
      </div>
    );
  }

  return (
    <div className="flex flex-col gap-6 p-6 max-w-[75vw] mx-auto w-full">
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
              existingSubmission.status === "Submitted"
                ? "bg-emerald-100 text-emerald-700"
                : "bg-muted text-muted-foreground"
            }
          >
            {existingSubmission.status}
          </Badge>
        )}
      </div>

      {/* ── Period selector ───────────────────────────────────────── */}
      <div className="flex items-center gap-6 rounded-xl border bg-card px-6 py-4">
        <div className="shrink-0">
          <p className="text-xs font-semibold text-muted-foreground uppercase tracking-widest">
            Counting Period
          </p>
          <p className="text-sm font-bold mt-0.5">
            {selectedPeriodId && selectedPeriod
              ? `${MONTHS_FULL[selectedPeriod.month]} ${selectedPeriod.year}`
              : "Select a period to begin"}
          </p>
        </div>
        <div className="h-8 w-px bg-border shrink-0" />
        {openPeriods.length === 0 ? (
          <p className="text-sm text-muted-foreground">
            No open periods available at this time.
          </p>
        ) : (
          <Select
            value={selectedPeriodId ? String(selectedPeriodId) : ""}
            onValueChange={handlePeriodChange}
          >
            <SelectTrigger className="w-52">
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
      </div>

      {/* ── Content area ─────────────────────────────────────────── */}
      <div className="flex flex-col gap-5">
        {/* ── Status banners ───────────────────────────────────────── */}
        {selectedPeriodId && isLocked && (
          <div className="flex items-center gap-3 rounded-lg border border-amber-200 bg-amber-50 px-4 py-3 text-amber-800">
            <Lock className="h-4 w-4 shrink-0" />
            <p className="text-sm font-medium">
              This period has been locked by the admin. No further changes can
              be made.
            </p>
          </div>
        )}
        {selectedPeriodId && isSubmitted && !isLocked && (
          <div className="flex items-center gap-3 rounded-lg border border-emerald-200 bg-emerald-50 px-4 py-3 text-emerald-800">
            <CheckCircle2 className="h-4 w-4 shrink-0" />
            <p className="text-sm font-medium">
              Your stock count has been submitted. The admin will review and
              adjust the system inventory.
            </p>
          </div>
        )}

        {/* ── No period selected placeholder ───────────────────────── */}
        {!selectedPeriodId && (
          <div className="flex flex-col items-center justify-center py-24 text-center">
            <div className="flex h-14 w-14 items-center justify-center rounded-2xl bg-muted mb-4">
              <PackageSearch className="h-7 w-7 text-muted-foreground" />
            </div>
            <p className="text-sm font-semibold">Select a counting period</p>
            <p className="text-xs text-muted-foreground mt-1">
              Choose a period above to begin your stock count
            </p>
          </div>
        )}

        {/* ── Main two-column layout ────────────────────────────────── */}
        {selectedPeriodId && (
          <>
            {isLoadingSubmission ? (
              <div className="flex items-center justify-center py-20">
                <Spinner className="size-5 text-slate-400" />
              </div>
            ) : (
              <Form {...form}>
                <form onSubmit={(e) => e.preventDefault()}>
                  <div className="grid grid-cols-1 lg:grid-cols-[1fr_260px] gap-5 items-start">
                    {/* ── Left: product table ───────────────────── */}
                    <div className="bg-card rounded-xl border">
                      {/* Table header */}
                      <div className="flex items-center justify-between px-5 py-4 border-b">
                        <div>
                          <p className="text-sm font-bold">Products to Count</p>
                          <p className="text-xs text-muted-foreground mt-0.5">
                            {fields.length}{" "}
                            {fields.length === 1 ? "product" : "products"} in
                            list
                          </p>
                        </div>
                        {!isReadOnly && (
                          <Button
                            type="button"
                            variant="outline"
                            size="sm"
                            className="gap-1.5 text-xs"
                            onClick={() =>
                              append({
                                productId: 0,
                                stockType: "Normal",
                                countedQuantity: 0,
                              })
                            }
                          >
                            <Plus className="h-3.5 w-3.5" />
                            Add Product
                          </Button>
                        )}
                      </div>

                      {fields.length === 0 ? (
                        /* ── Empty state ── */
                        <div className="flex flex-col items-center justify-center gap-4 py-16 px-6 text-center">
                          <div className="flex h-14 w-14 items-center justify-center rounded-2xl bg-muted">
                            <PackageSearch className="h-7 w-7 text-muted-foreground" />
                          </div>
                          <div>
                            <p className="text-sm font-semibold">
                              No products added yet
                            </p>
                            <p className="text-xs text-muted-foreground mt-1">
                              Click &ldquo;Add Product&rdquo; to start entering
                              your physical counts.
                            </p>
                          </div>
                          {!isReadOnly && (
                            <Button
                              type="button"
                              variant="outline"
                              size="sm"
                              className="gap-2 mt-1"
                              onClick={() =>
                                append({
                                  productId: 0,
                                  stockType: "Normal",
                                  countedQuantity: 0,
                                })
                              }
                            >
                              <Plus className="h-3.5 w-3.5" />
                              Add First Product
                            </Button>
                          )}
                        </div>
                      ) : (
                        <>
                          <div>
                            <table className="w-full text-sm table-fixed">
                              <thead>
                                <tr className="bg-muted/30 border-b">
                                  <th className="py-2.5 px-3 text-left text-[10px] font-bold text-muted-foreground uppercase tracking-wider w-[3%]">
                                    #
                                  </th>
                                  <th className="py-2.5 px-3 text-left text-[10px] font-bold text-muted-foreground uppercase tracking-wider w-[47%]">
                                    Product
                                  </th>
                                  <th className="py-2.5 px-3 text-left text-[10px] font-bold text-muted-foreground uppercase tracking-wider w-[22%]">
                                    Stock Type
                                  </th>
                                  <th className="py-2.5 px-3 text-center text-[10px] font-bold text-muted-foreground uppercase tracking-wider w-[20%]">
                                    Qty on Hand
                                  </th>
                                  {!isReadOnly && (
                                    <th className="py-2.5 px-3 w-[8%]" />
                                  )}
                                </tr>
                              </thead>
                              <tbody>
                                {fields.map((field, index) => {
                                  const line = watchedLines[index];
                                  return (
                                    <tr
                                      key={field.id}
                                      className="border-b hover:bg-muted/20 transition-colors"
                                    >
                                      {/* # */}
                                      <td className="py-3 px-3">
                                        <span className="text-[11px] font-bold text-muted-foreground/50 font-mono">
                                          {String(index + 1).padStart(2, "0")}
                                        </span>
                                      </td>

                                      {/* Product */}
                                      <td className="py-3 px-3 w-full min-w-0">
                                        {isReadOnly ? (
                                          <div>
                                            <p className="text-sm font-semibold">
                                              {existingSubmission?.lines[index]?.productCode}
                                            </p>
                                            <p className="text-xs text-muted-foreground">
                                              {existingSubmission?.lines[index]?.productDescription}
                                            </p>
                                          </div>
                                        ) : (
                                          <FormField
                                            control={form.control}
                                            name={`lines.${index}.productId`}
                                            render={({ field: f }) => (
                                              <FormItem className="mb-0 w-full">
                                                <FormControl className="w-full">
                                                  <AsyncSelect<ProductForSelect>
                                                    label="product"
                                                    placeholder="Search product..."
                                                    fetcher={fetchProducts}
                                                    value={
                                                      f.value
                                                        ? String(f.value)
                                                        : ""
                                                    }
                                                    onChange={(v) =>
                                                      f.onChange(
                                                        v ? Number(v) : 0,
                                                      )
                                                    }
                                                    getOptionValue={(p) =>
                                                      String(p.id)
                                                    }
                                                    getDisplayValue={(p) =>
                                                      `${p.code} — ${p.itemDescription}`
                                                    }
                                                    renderOption={(p) => (
                                                      <div>
                                                        <p className="font-semibold text-sm">
                                                          {p.code}
                                                        </p>
                                                        <p className="text-xs text-muted-foreground">
                                                          {p.itemDescription}
                                                        </p>
                                                      </div>
                                                    )}
                                                    width="100%"
                                                    preload
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
                                      <td className="py-3 px-3">
                                        {isReadOnly ? (
                                          <Badge
                                            variant="outline"
                                            className="text-xs font-medium"
                                          >
                                            {line?.stockType}
                                          </Badge>
                                        ) : (
                                          <FormField
                                            control={form.control}
                                            name={`lines.${index}.stockType`}
                                            render={({ field: f }) => (
                                              <FormItem className="mb-0">
                                                <Select
                                                  value={f.value}
                                                  onValueChange={f.onChange}
                                                >
                                                  <FormControl>
                                                    <SelectTrigger className="h-8 w-full text-xs">
                                                      <SelectValue />
                                                    </SelectTrigger>
                                                  </FormControl>
                                                  <SelectContent>
                                                    <SelectItem value="Normal">
                                                      Normal
                                                    </SelectItem>
                                                    <SelectItem value="FreeIssue">
                                                      Free Issue
                                                    </SelectItem>
                                                  </SelectContent>
                                                </Select>
                                                <FormMessage />
                                              </FormItem>
                                            )}
                                          />
                                        )}
                                      </td>

                                      {/* Quantity */}
                                      <td className="py-3 px-3 text-center">
                                        {isReadOnly ? (
                                          <span className="font-mono font-bold text-sm">
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
                                                    className="h-8 w-full text-center font-mono font-bold"
                                                    {...f}
                                                    onChange={(e) =>
                                                      f.onChange(
                                                        parseFloat(
                                                          e.target.value,
                                                        ) || 0,
                                                      )
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
                                        <td className="py-3 px-3 text-center">
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
                                  );
                                })}
                              </tbody>
                            </table>
                          </div>

                          {/* Add another — inline */}
                          {!isReadOnly && (
                            <div className="px-5 py-3 border-t bg-muted/20">
                              <Button
                                type="button"
                                variant="ghost"
                                size="sm"
                                className="h-auto p-0 text-xs text-muted-foreground font-semibold hover:bg-transparent hover:text-foreground gap-1.5"
                                onClick={() =>
                                  append({
                                    productId: 0,
                                    stockType: "Normal",
                                    countedQuantity: 0,
                                  })
                                }
                              >
                                <Plus className="h-3 w-3" />
                                Add another product
                              </Button>
                            </div>
                          )}
                        </>
                      )}
                    </div>

                    {/* ── Right: action panel ────────────────────── */}
                    <div className="lg:sticky lg:top-6 space-y-3">
                      <Card>
                        <CardHeader className="pb-2">
                          <CardTitle className="text-base">Summary</CardTitle>
                        </CardHeader>
                        <CardContent className="space-y-3 text-sm">
                          <div className="flex justify-between items-center">
                            <span className="text-muted-foreground">
                              Period
                            </span>
                            <span className="font-semibold text-right">
                              {selectedPeriod
                                ? `${MONTHS_FULL[selectedPeriod.month]} ${selectedPeriod.year}`
                                : "—"}
                            </span>
                          </div>
                          <div className="flex justify-between items-center">
                            <span className="text-muted-foreground">
                              Products
                            </span>
                            <span className="font-semibold">{totalLines}</span>
                          </div>
                          <div className="flex justify-between items-center">
                            <span className="text-muted-foreground">
                              Total Qty
                            </span>
                            <span className="font-semibold font-mono">
                              {totalCounted.toFixed(2)}
                            </span>
                          </div>
                          <div className="flex justify-between items-center">
                            <span className="text-muted-foreground">
                              Status
                            </span>
                            {existingSubmission ? (
                              <Badge
                                className={
                                  existingSubmission.status === "Submitted"
                                    ? "bg-emerald-100 text-emerald-700"
                                    : "bg-muted text-muted-foreground"
                                }
                              >
                                {existingSubmission.status}
                              </Badge>
                            ) : (
                              <span className="text-muted-foreground text-xs">
                                Not started
                              </span>
                            )}
                          </div>
                          <Separator />
                          <p className="text-xs text-muted-foreground leading-relaxed">
                            Enter the physical quantity you see in your
                            warehouse. System stock is recorded at submission
                            time.
                          </p>
                        </CardContent>
                      </Card>

                      {/* Actions */}
                      {!isReadOnly && (
                        <Button
                          type="button"
                          className="w-full gap-2"
                          disabled={isPending || fields.length === 0}
                          onClick={form.handleSubmit((data) => submitCount(data))}
                        >
                          {isSubmitting ? (
                            <Spinner className="h-4 w-4" />
                          ) : (
                            <Send className="h-4 w-4" />
                          )}
                          Submit Count
                        </Button>
                      )}
                    </div>
                  </div>
                </form>
              </Form>
            )}
          </>
        )}
      </div>
    </div>
  );
}
