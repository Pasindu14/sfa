'use client'

import { Skeleton } from '@/components/ui/skeleton'
import { TrendingUp, ReceiptText, BadgeCheck, Clock4 } from 'lucide-react'
import { useMyBillingsTodaySummary } from '../../hooks/distributor-billing.hooks'
import { DistributorBillingTable } from '../table/distributor-billing-table'

function formatCurrency(amount: number) {
  return new Intl.NumberFormat('en-LK', {
    style: 'currency',
    currency: 'LKR',
    minimumFractionDigits: 2,
  }).format(amount)
}

function formatDate(d: Date) {
  return d.toLocaleDateString('en-US', { weekday: 'long', day: 'numeric', month: 'long', year: 'numeric' })
}

interface StatCardProps {
  icon: React.ElementType
  label: string
  value: React.ReactNode
  sub?: string
  accent?: string
}

function StatCard({ icon: Icon, label, value, sub, accent = 'bg-primary/8 text-primary' }: StatCardProps) {
  return (
    <div className="flex items-start gap-3 rounded-xl border bg-card px-4 py-3.5 shadow-sm">
      <div className={`mt-0.5 flex h-8 w-8 shrink-0 items-center justify-center rounded-lg ${accent}`}>
        <Icon className="h-4 w-4" />
      </div>
      <div className="min-w-0 flex-1">
        <p className="text-[11px] font-medium uppercase tracking-widest text-muted-foreground">{label}</p>
        <p className="mt-0.5 truncate text-lg font-bold tabular-nums leading-tight tracking-tight">{value}</p>
        {sub && <p className="mt-0.5 text-[11px] text-muted-foreground">{sub}</p>}
      </div>
    </div>
  )
}

function SummaryStats() {
  const { data, isLoading } = useMyBillingsTodaySummary()

  if (isLoading) {
    return (
      <div className="grid grid-cols-1 gap-3 sm:grid-cols-3">
        {[1, 2, 3].map((i) => (
          <Skeleton key={i} className="h-[72px] rounded-xl" />
        ))}
      </div>
    )
  }

  const stats = data ?? { totalRevenue: 0, totalCount: 0, approvedRevenue: 0, approvedCount: 0, submittedCount: 0 }

  return (
    <div className="grid grid-cols-1 gap-3 sm:grid-cols-3">
      <StatCard
        icon={TrendingUp}
        label="Today's Revenue"
        value={formatCurrency(stats.totalRevenue)}
        sub={`${stats.totalCount} bill${stats.totalCount !== 1 ? 's' : ''} issued today`}
        accent="bg-emerald-50 text-emerald-600 dark:bg-emerald-950/40 dark:text-emerald-400"
      />
      <StatCard
        icon={BadgeCheck}
        label="Approved"
        value={formatCurrency(stats.approvedRevenue)}
        sub={`${stats.approvedCount} bill${stats.approvedCount !== 1 ? 's' : ''} approved`}
        accent="bg-blue-50 text-blue-600 dark:bg-blue-950/40 dark:text-blue-400"
      />
      <StatCard
        icon={Clock4}
        label="Pending Review"
        value={String(stats.submittedCount)}
        sub={stats.submittedCount > 0 ? 'Awaiting approval' : 'All bills reviewed'}
        accent="bg-amber-50 text-amber-600 dark:bg-amber-950/40 dark:text-amber-400"
      />
    </div>
  )
}

export function DistributorBillingPage() {
  const now = new Date()

  return (
    <div className="flex flex-col gap-5 p-6">

      {/* Page header */}
      <div className="flex flex-col gap-5 rounded-xl border bg-card px-6 py-5 shadow-sm">
        <div className="flex items-start justify-between gap-4">
          <div className="flex items-start gap-3.5">
            <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-xl bg-primary/10">
              <ReceiptText className="h-5 w-5 text-primary" />
            </div>
            <div>
              <h1 className="text-xl font-bold tracking-tight">Bills</h1>
              <p className="text-sm text-muted-foreground">Sales transactions issued by your representatives</p>
            </div>
          </div>
          <div className="shrink-0 rounded-md border bg-muted/50 px-3 py-1.5 text-right">
            <p className="text-[10px] font-semibold uppercase tracking-widest text-muted-foreground">Today</p>
            <p className="text-xs font-medium text-foreground">{formatDate(now)}</p>
          </div>
        </div>

        {/* Today's summary stats */}
        <SummaryStats />
      </div>

      {/* Table */}
      <div className="[&_td:not(:last-child)]:border-r [&_th:not(:last-child)]:border-r">
        <DistributorBillingTable />
      </div>
    </div>
  )
}
