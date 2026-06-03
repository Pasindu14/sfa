'use client'

import { useState } from 'react'
import { ArrowLeft, Lock, LockOpen, TrendingUp, TrendingDown, BarChart3, CheckCircle2, Package, SlidersHorizontal } from 'lucide-react'
import Link from 'next/link'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { Spinner } from '@/components/ui/spinner'
import { AsyncSelect } from '@/components/async-select'
import { getDistributorsAction } from '@/features/distributor/actions/distributor.actions'
import type { DistributorDto } from '@/features/distributor/schema/distributor.schema'
import { usePeriod, useSubmissionForAdmin } from '../../hooks/stock-taking.hooks'
import { useAdjustDialog } from '../../store'
import { AdjustLineDialog } from '../dialogs/stock-taking-dialogs'
import type { StockTakingLineDto } from '../../schema/stock-taking.schema'

const MONTHS = [
  '', 'January', 'February', 'March', 'April', 'May', 'June',
  'July', 'August', 'September', 'October', 'November', 'December',
]

async function fetchDistributors(search?: string): Promise<DistributorDto[]> {
  if (!search || search.trim().length === 0) return []
  const result = await getDistributorsAction(1, 50, search.trim())
  if (!result.success) return []
  return result.data.distributors
}

// ── Stat chip ─────────────────────────────────────────────────────────────

function StatChip({
  label,
  value,
  icon: Icon,
  color,
}: {
  label: string
  value: string | number
  icon: React.ElementType
  color: string
}) {
  return (
    <div className={`flex items-center gap-3 rounded-xl border px-4 py-3 bg-white shadow-sm ${color}`}>
      <div className="flex h-8 w-8 shrink-0 items-center justify-center rounded-lg bg-current/10">
        <Icon className="h-4 w-4" />
      </div>
      <div>
        <p className="text-xs font-medium text-muted-foreground leading-none mb-0.5">{label}</p>
        <p className="text-lg font-bold tabular-nums leading-none">{value}</p>
      </div>
    </div>
  )
}

// ── Variance pill ─────────────────────────────────────────────────────────

function VarianceCell({ variance }: { variance: number }) {
  if (variance === 0) {
    return (
      <div className="flex items-center justify-center gap-1.5">
        <div className="h-1.5 w-1.5 rounded-full bg-emerald-500" />
        <span className="text-xs font-mono font-medium text-emerald-600">0.0000</span>
      </div>
    )
  }
  const isPositive = variance > 0
  return (
    <div className="flex items-center justify-center gap-1.5">
      <div className={`h-1.5 w-1.5 rounded-full ${isPositive ? 'bg-blue-500' : 'bg-red-500'}`} />
      <span className={`text-xs font-mono font-semibold ${isPositive ? 'text-blue-600' : 'text-red-600'}`}>
        {isPositive ? '+' : ''}{variance.toFixed(4)}
      </span>
    </div>
  )
}

// ── Line row ─────────────────────────────────────────────────────────────

function LineRow({ line, index }: { line: StockTakingLineDto; index: number }) {
  const { open } = useAdjustDialog()
  const isNormal = line.stockType === 'Normal'

  return (
    <tr
      className="group border-b border-slate-100 last:border-0 transition-colors hover:bg-slate-50/80"
      style={{ animationDelay: `${index * 30}ms` }}
    >
      {/* # */}
      <td className="py-3.5 pl-6 pr-2 text-xs font-mono text-slate-400 w-8">{String(index + 1).padStart(2, '0')}</td>

      {/* Product */}
      <td className="py-3.5 px-3">
        <div className="flex items-center gap-2.5">
          <div className="flex h-7 w-7 shrink-0 items-center justify-center rounded-md bg-slate-100 text-slate-500">
            <Package className="h-3.5 w-3.5" />
          </div>
          <div>
            <p className="text-sm font-semibold text-slate-800 leading-none">{line.productCode}</p>
            <p className="text-xs text-slate-500 mt-0.5 max-w-[200px] truncate">{line.productDescription}</p>
          </div>
        </div>
      </td>

      {/* Stock type */}
      <td className="py-3.5 px-3 text-center">
        <span className={`inline-flex items-center rounded-full px-2 py-0.5 text-[10px] font-semibold uppercase tracking-wider ${
          isNormal
            ? 'bg-slate-100 text-slate-600'
            : 'bg-violet-50 text-violet-600 ring-1 ring-violet-200'
        }`}>
          {isNormal ? 'Normal' : 'Free Issue'}
        </span>
      </td>

      {/* Counted */}
      <td className="py-3.5 px-3 text-right">
        <span className="text-sm font-mono font-semibold text-slate-800">
          {line.countedQuantity.toFixed(4)}
        </span>
      </td>

      {/* System */}
      <td className="py-3.5 px-3 text-right">
        <span className="text-sm font-mono text-slate-500">
          {line.systemQuantity.toFixed(4)}
        </span>
      </td>

      {/* Variance */}
      <td className="py-3.5 px-3">
        <VarianceCell variance={line.variance} />
      </td>

      {/* Adjusted */}
      <td className="py-3.5 px-3 text-center">
        {line.isAdjusted ? (
          <div className="flex flex-col items-center gap-0.5">
            <span className="inline-flex items-center gap-1 rounded-full bg-emerald-50 px-2 py-0.5 text-[10px] font-semibold text-emerald-700 ring-1 ring-emerald-200">
              <CheckCircle2 className="h-2.5 w-2.5" />
              Done
            </span>
            {line.adjustedQuantity !== null && (
              <span className="text-[10px] font-mono text-slate-400">→ {line.adjustedQuantity}</span>
            )}
          </div>
        ) : (
          <span className="text-slate-300 text-sm">—</span>
        )}
      </td>

      {/* Action */}
      <td className="py-3.5 pl-3 pr-6 text-center">
        <Button
          variant="ghost"
          size="sm"
          className="h-7 px-3 text-xs font-medium text-slate-500 hover:text-slate-900 hover:bg-slate-100 opacity-0 group-hover:opacity-100 transition-opacity"
          onClick={() => open(line.id, line.countedQuantity)}
        >
          <SlidersHorizontal className="h-3 w-3 mr-1.5" />
          Adjust
        </Button>
      </td>
    </tr>
  )
}

// ── Main page ─────────────────────────────────────────────────────────────

interface StockTakingReviewPageProps {
  periodId: number
}

export function StockTakingReviewPage({ periodId }: StockTakingReviewPageProps) {
  const [selectedDistributorId, setSelectedDistributorId] = useState<number | null>(null)

  const { data: period, isLoading: isLoadingPeriod } = usePeriod(periodId)
  const { data: submission, isLoading: isLoadingSubmission } = useSubmissionForAdmin(
    periodId,
    selectedDistributorId,
  )

  const isLocked = period?.status === 'Locked'

  // Derived stats
  const lines = submission?.lines ?? []
  const linesWithVariance = lines.filter((l) => l.variance !== 0).length
  const totalSurplus = lines.filter((l) => l.variance > 0).reduce((s, l) => s + l.variance, 0)
  const totalDeficit = lines.filter((l) => l.variance < 0).reduce((s, l) => s + l.variance, 0)
  const adjustedCount = lines.filter((l) => l.isAdjusted).length

  return (
    <div className="flex flex-col gap-6 p-6">

      {/* ── Header ─────────────────────────────────────────── */}
      <div className="flex items-center gap-4 bg-muted/90 p-10 rounded-lg">
        <Link href="/stock-taking">
          <Button variant="ghost" size="icon" className="h-9 w-9 shrink-0">
            <ArrowLeft className="h-4 w-4" />
          </Button>
        </Link>
        <div className="flex-1">
          {isLoadingPeriod ? (
            <Spinner className="size-5" />
          ) : period ? (
            <>
              <div className="flex items-center gap-3">
                <h1 className="text-3xl font-bold tracking-tight">
                  {MONTHS[period.month]} {period.year}
                </h1>
                <Badge className={isLocked ? 'bg-amber-100 text-amber-700' : 'bg-emerald-100 text-emerald-700'}>
                  {isLocked ? <Lock className="h-3 w-3 mr-1" /> : <LockOpen className="h-3 w-3 mr-1" />}
                  {period.status}
                </Badge>
              </div>
              <p className="text-muted-foreground">Review distributor stock counts and apply adjustments</p>
            </>
          ) : null}
        </div>
      </div>

      {/* ── Main content ─────────────────────────────────────────── */}
      <div className="space-y-5">

        {/* Distributor search bar */}
        <div className="flex items-center gap-4 rounded-xl border border-slate-200 bg-white px-5 py-4 shadow-sm">
          <div className="text-xs font-semibold uppercase tracking-widest text-slate-400 shrink-0 w-32">
            Distributor
          </div>
          <div className="h-5 w-px bg-slate-200 shrink-0" />
          <div className="flex-1 max-w-md">
            <AsyncSelect<DistributorDto>
              label="Distributor"
              placeholder="Search by name..."
              fetcher={fetchDistributors}
              value={selectedDistributorId?.toString() ?? ''}
              onChange={(val) => setSelectedDistributorId(val ? Number(val) : null)}
              getOptionValue={(d) => d.id.toString()}
              getDisplayValue={(d) => d.name}
              renderOption={(d) => (
                <div className="py-0.5">
                  <p className="font-medium text-sm">{d.name}</p>
                  <p className="text-xs text-muted-foreground">{d.email}</p>
                </div>
              )}
              clearable
            />
          </div>
          {submission && (
            <span className={`ml-auto shrink-0 rounded-full px-3 py-1 text-xs font-semibold ${
              submission.status === 'Submitted'
                ? 'bg-emerald-50 text-emerald-700 ring-1 ring-emerald-200'
                : 'bg-slate-100 text-slate-500'
            }`}>
              {submission.status}
            </span>
          )}
        </div>

        {/* Stat chips — animate in when submission loaded */}
        {selectedDistributorId && !isLoadingSubmission && submission && lines.length > 0 && (
          <div className="grid grid-cols-2 md:grid-cols-4 gap-3">
            <StatChip
              label="Total Products"
              value={lines.length}
              icon={Package}
              color="text-slate-600"
            />
            <StatChip
              label="With Variance"
              value={linesWithVariance}
              icon={BarChart3}
              color={linesWithVariance > 0 ? 'text-amber-600' : 'text-emerald-600'}
            />
            <StatChip
              label="Surplus"
              value={`+${totalSurplus.toFixed(2)}`}
              icon={TrendingUp}
              color="text-blue-600"
            />
            <StatChip
              label="Deficit"
              value={totalDeficit.toFixed(2)}
              icon={TrendingDown}
              color="text-red-600"
            />
          </div>
        )}

        {/* Lines table */}
        {selectedDistributorId && (
          <div className="rounded-xl border border-slate-200 bg-white shadow-sm overflow-hidden">

            {/* Table header bar */}
            <div className="flex items-center justify-between px-6 py-4 border-b border-slate-100">
              <div>
                <h2 className="text-sm font-semibold text-slate-800">Stock Count Lines</h2>
                {submission && (
                  <p className="text-xs text-slate-400 mt-0.5">
                    {lines.length} product{lines.length !== 1 ? 's' : ''} &nbsp;·&nbsp; {adjustedCount} adjusted
                  </p>
                )}
              </div>
            </div>

            {isLoadingSubmission ? (
              <div className="flex items-center justify-center py-20">
                <Spinner className="size-5 text-slate-400" />
              </div>
            ) : !submission ? (
              <div className="flex flex-col items-center justify-center py-20 gap-3">
                <div className="flex h-12 w-12 items-center justify-center rounded-full bg-slate-100">
                  <Package className="h-5 w-5 text-slate-400" />
                </div>
                <p className="text-sm text-slate-500 font-medium">No submission found</p>
                <p className="text-xs text-slate-400">This distributor hasn&apos;t submitted for this period yet.</p>
              </div>
            ) : lines.length === 0 ? (
              <div className="flex items-center justify-center py-20 text-sm text-slate-400">
                No lines in this submission.
              </div>
            ) : (
              <div className="overflow-x-auto">
                <table className="w-full">
                  <thead>
                    <tr className="border-b border-slate-100 bg-slate-50/60">
                      <th className="py-2.5 pl-6 pr-2 text-left text-[10px] font-semibold uppercase tracking-widest text-slate-400 w-8">#</th>
                      <th className="py-2.5 px-3 text-left text-[10px] font-semibold uppercase tracking-widest text-slate-400">Product</th>
                      <th className="py-2.5 px-3 text-center text-[10px] font-semibold uppercase tracking-widest text-slate-400">Type</th>
                      <th className="py-2.5 px-3 text-right text-[10px] font-semibold uppercase tracking-widest text-slate-400">Counted</th>
                      <th className="py-2.5 px-3 text-right text-[10px] font-semibold uppercase tracking-widest text-slate-400">System</th>
                      <th className="py-2.5 px-3 text-center text-[10px] font-semibold uppercase tracking-widest text-slate-400">Variance</th>
                      <th className="py-2.5 px-3 text-center text-[10px] font-semibold uppercase tracking-widest text-slate-400">Status</th>
                      <th className="py-2.5 pl-3 pr-6 text-center text-[10px] font-semibold uppercase tracking-widest text-slate-400">Action</th>
                    </tr>
                  </thead>
                  <tbody>
                    {lines.map((line, i) => (
                      <LineRow key={line.id} line={line} index={i} />
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </div>
        )}

        {/* Empty state — no distributor selected */}
        {!selectedDistributorId && (
          <div className="flex flex-col items-center justify-center py-24 gap-4">
            <div className="flex h-16 w-16 items-center justify-center rounded-2xl bg-slate-100">
              <BarChart3 className="h-8 w-8 text-slate-400" />
            </div>
            <div className="text-center">
              <p className="text-sm font-semibold text-slate-700">No distributor selected</p>
              <p className="text-xs text-slate-400 mt-1">Search for a distributor above to view their stock count</p>
            </div>
          </div>
        )}
      </div>

      <AdjustLineDialog />
    </div>
  )
}
