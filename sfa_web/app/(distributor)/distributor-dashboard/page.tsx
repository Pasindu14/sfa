"use client"

import Link from "next/link"
import { useSession } from "next-auth/react"
import {
  ShoppingCart, Package, FileText, BarChart3, ReceiptText,
  TrendingUp, Clock4, ArrowUpRight, AlertTriangle,
  CheckCircle2, Boxes, Activity, ChevronRight,
} from "lucide-react"
import { Skeleton } from "@/components/ui/skeleton"
import { Bar, BarChart, CartesianGrid, XAxis, YAxis, ResponsiveContainer, Tooltip } from "recharts"
import { useMyBillingsTodaySummary, useMyBillingWeeklyTrend, useMyPendingBillingCount } from "@/features/distributor-billings/hooks/distributor-billing.hooks"
import { useMyStockSummary } from "@/features/distributor-stock/hooks/distributor-stock.hooks"
import { useMyGrnPendingCount } from "@/features/distributor-grn/hooks/distributor-grn.hooks"
import { useMyPurchaseOrderStats, useMyDistributorProfile } from "@/features/distributor-purchase-orders/hooks/distributor-purchase-order.hooks"

// ── Formatters ─────────────────────────────────────────────────────────────

function formatCurrency(amount: number) {
  if (amount >= 1_000_000) return `LKR ${(amount / 1_000_000).toFixed(1)}M`
  if (amount >= 1_000) return `LKR ${(amount / 1_000).toFixed(0)}K`
  return new Intl.NumberFormat("en-LK", { style: "currency", currency: "LKR", minimumFractionDigits: 0, maximumFractionDigits: 0 }).format(amount)
}

function formatCurrencyFull(amount: number) {
  return new Intl.NumberFormat("en-LK", { style: "currency", currency: "LKR", minimumFractionDigits: 0, maximumFractionDigits: 0 }).format(amount)
}

function formatDate(d: Date) {
  return d.toLocaleDateString("en-US", { weekday: "long", day: "numeric", month: "long", year: "numeric" })
}

const CATEGORY_STYLES: Record<string, string> = {
  A: "bg-amber-100 text-amber-800 dark:bg-amber-900/40 dark:text-amber-300 border border-amber-200 dark:border-amber-700",
  B: "bg-zinc-100 text-zinc-700 dark:bg-zinc-800 dark:text-zinc-300 border border-zinc-200 dark:border-zinc-600",
  C: "bg-orange-100 text-orange-700 dark:bg-orange-900/40 dark:text-orange-300 border border-orange-200 dark:border-orange-700",
  D: "bg-blue-100 text-blue-700 dark:bg-blue-900/40 dark:text-blue-300 border border-blue-200 dark:border-blue-700",
}

// ── Section heading with rule ──────────────────────────────────────────────

function SectionHeading({ children }: { children: React.ReactNode }) {
  return (
    <div className="flex items-center gap-3 mb-4">
      <p className="text-[10px] font-bold uppercase tracking-[0.25em] text-muted-foreground whitespace-nowrap">
        {children}
      </p>
      <div className="h-px flex-1 bg-border" />
    </div>
  )
}

// ── KPI Stat Card — left-border accent ────────────────────────────────────

interface StatCardProps {
  icon: React.ElementType
  label: string
  value: React.ReactNode
  sub: string
  iconClass: string
  borderClass: string
  urgent?: boolean
}

function StatCard({ icon: Icon, label, value, sub, borderClass, urgent }: StatCardProps) {
  return (
    <div className={`relative flex flex-col justify-between gap-4 rounded-xl bg-card border border-l-[3px] ${borderClass} px-5 py-5 shadow-sm overflow-hidden min-h-[110px]`}>
      {urgent && (
        <span className="absolute top-2.5 right-2.5 flex h-2 w-2">
          <span className="animate-ping absolute inline-flex h-full w-full rounded-full bg-amber-400 opacity-75" />
          <span className="relative inline-flex rounded-full h-2 w-2 bg-amber-500" />
        </span>
      )}
      <Icon className="absolute bottom-2 right-3 h-14 w-14 opacity-[0.04] pointer-events-none select-none" />
      <p className="text-[10px] font-semibold uppercase tracking-[0.2em] text-muted-foreground">{label}</p>
      <div>
        <p className="text-[1.6rem] font-bold tabular-nums tracking-tight leading-none">{value}</p>
        <p className="mt-1.5 text-[11px] text-muted-foreground leading-tight">{sub}</p>
      </div>
    </div>
  )
}

// ── Section 2: Today's Pulse ───────────────────────────────────────────────

function TodaysPulse() {
  const { data: billing, isLoading: billingLoading } = useMyBillingsTodaySummary()
  const { data: pendingCount, isLoading: pendingLoading } = useMyPendingBillingCount()
  const { data: poStats, isLoading: poLoading } = useMyPurchaseOrderStats()
  const { data: grnPending, isLoading: grnLoading } = useMyGrnPendingCount()

  const isLoading = billingLoading || pendingLoading || poLoading || grnLoading

  if (isLoading) {
    return (
      <div className="grid grid-cols-2 gap-3 lg:grid-cols-4">
        {[1, 2, 3, 4].map((i) => <Skeleton key={i} className="h-[110px] rounded-xl" />)}
      </div>
    )
  }

  const b = billing ?? { totalRevenue: 0, totalCount: 0 }
  const pending = pendingCount ?? 0
  const poNeedAction = (poStats?.pendingRepApproval ?? 0) + (poStats?.pendingManagerApproval ?? 0) + (poStats?.pendingAcknowledgement ?? 0)
  const grnCount = grnPending ?? 0

  return (
    <div className="grid grid-cols-2 gap-3 lg:grid-cols-4">
      <StatCard icon={TrendingUp} label="Today's Revenue" value={formatCurrency(b.totalRevenue)}
        sub={`${b.totalCount} bill${b.totalCount !== 1 ? "s" : ""} issued`}
        iconClass="" borderClass="border-l-emerald-500" />
      <StatCard icon={Clock4} label="Bills Pending" value={pending}
        sub={pending > 0 ? "Awaiting your approval" : "All reviewed"}
        iconClass="" borderClass="border-l-amber-500" urgent={pending > 0} />
      <StatCard icon={ShoppingCart} label="POs Need Action" value={poNeedAction}
        sub={poNeedAction > 0 ? "In pipeline" : "Pipeline clear"}
        iconClass="" borderClass="border-l-blue-500" urgent={poNeedAction > 0} />
      <StatCard icon={FileText} label="GRNs Pending" value={grnCount}
        sub={grnCount > 0 ? "Awaiting confirmation" : "All confirmed"}
        iconClass="" borderClass="border-l-violet-500" urgent={grnCount > 0} />
    </div>
  )
}

// ── Section 3: Weekly Billing Trend ───────────────────────────────────────

function WeeklyTrend() {
  const { data, isLoading } = useMyBillingWeeklyTrend()

  if (isLoading) return <Skeleton className="h-[240px] w-full rounded-xl" />

  const hasData = data && data.some(d => d.approved > 0 || d.pending > 0)

  return (
    <div className="rounded-xl border bg-card shadow-sm overflow-hidden">
      <div className="flex items-center justify-between px-6 py-4 border-b">
        <div>
          <p className="text-sm font-semibold leading-none">Billing Revenue</p>
          <p className="text-[11px] text-muted-foreground mt-1">Last 7 days</p>
        </div>
        <div className="flex items-center gap-4 text-[10px] text-muted-foreground">
          <span className="flex items-center gap-1.5"><span className="h-2.5 w-2.5 rounded-sm bg-emerald-500" />Approved</span>
          <span className="flex items-center gap-1.5"><span className="h-2.5 w-2.5 rounded-sm bg-amber-400" />Pending</span>
        </div>
      </div>
      <div className="px-4 py-4">
        {!hasData ? (
          <div className="flex h-[160px] items-center justify-center">
            <p className="text-xs text-muted-foreground">No billing data for the past 7 days</p>
          </div>
        ) : (
          <div className="h-[190px]">
            <ResponsiveContainer width="100%" height="100%">
              <BarChart data={data} barCategoryGap="28%" barGap={2}>
                <CartesianGrid vertical={false} strokeDasharray="3 3" stroke="hsl(var(--border))" strokeOpacity={0.6} />
                <XAxis dataKey="day" tick={{ fontSize: 10, fill: "hsl(var(--muted-foreground))" }} axisLine={false} tickLine={false} />
                <YAxis tickFormatter={v => formatCurrency(v)} tick={{ fontSize: 9, fill: "hsl(var(--muted-foreground))" }} axisLine={false} tickLine={false} width={58} />
                <Tooltip
                  cursor={{ fill: "hsl(var(--muted))", opacity: 0.5 }}
                  content={({ active, payload, label }) => {
                    if (!active || !payload?.length) return null
                    const entry = data?.find(d => d.day === label)
                    return (
                      <div className="rounded-lg border bg-popover px-3 py-2 text-xs shadow-lg">
                        <p className="font-semibold mb-1.5 text-foreground">{entry?.date ?? label}</p>
                        {payload.map((p) => (
                          <div key={p.dataKey} className="flex items-center gap-2 justify-between mt-1">
                            <span className="flex items-center gap-1.5 text-muted-foreground capitalize">
                              <span className="h-1.5 w-1.5 rounded-full" style={{ backgroundColor: p.color }} />
                              {p.name}
                            </span>
                            <span className="font-mono font-medium ml-6 text-foreground">{formatCurrencyFull(p.value as number)}</span>
                          </div>
                        ))}
                      </div>
                    )
                  }}
                />
                <Bar dataKey="approved" name="Approved" fill="#10b981" radius={[3, 3, 0, 0]} />
                <Bar dataKey="pending" name="Pending" fill="#fbbf24" radius={[3, 3, 0, 0]} />
              </BarChart>
            </ResponsiveContainer>
          </div>
        )}
      </div>
    </div>
  )
}

// ── Section 4: Action Required ────────────────────────────────────────────

interface ActionPanelProps {
  icon: React.ElementType
  title: string
  count: number | undefined
  isLoading: boolean
  description: string
  href: string
  accentClass: string
  num: string
}

function ActionPanel({ icon: Icon, title, count, isLoading, description, href, accentClass, num }: ActionPanelProps) {
  const hasAction = !isLoading && (count ?? 0) > 0

  return (
    <Link href={href} className="group relative flex items-center gap-4 rounded-xl border bg-card px-5 py-4 shadow-sm transition-all hover:border-primary/40 hover:shadow-md hover:-translate-y-px overflow-hidden">
      <span className="pointer-events-none select-none absolute right-4 top-1/2 -translate-y-1/2 text-[5rem] font-black leading-none tabular-nums text-foreground/[0.035]">
        {num}
      </span>
      <div className={`relative z-10 flex h-10 w-10 shrink-0 items-center justify-center rounded-xl ${accentClass}`}>
        {hasAction ? <AlertTriangle className="h-4.5 w-4.5" /> : <CheckCircle2 className="h-4.5 w-4.5" />}
      </div>
      <div className="relative z-10 flex-1 min-w-0">
        <div className="flex items-center gap-2 flex-wrap">
          <p className="text-sm font-semibold leading-none">{title}</p>
          {isLoading
            ? <Skeleton className="h-4 w-6 rounded" />
            : hasAction
              ? <span className={`rounded-full px-2 py-0.5 text-[10px] font-bold leading-none ${accentClass}`}>{count}</span>
              : <span className="text-[10px] text-emerald-600 dark:text-emerald-400 font-medium">All clear</span>
          }
        </div>
        <p className="mt-1 text-[11px] text-muted-foreground">{description}</p>
      </div>
      <ChevronRight className="relative z-10 h-4 w-4 shrink-0 text-muted-foreground/30 group-hover:text-primary transition-colors" />
    </Link>
  )
}

function ActionRequired() {
  const { data: pendingCount, isLoading: pendingLoading } = useMyPendingBillingCount()
  const { data: poStats, isLoading: poLoading } = useMyPurchaseOrderStats()
  const { data: grnPending, isLoading: grnLoading } = useMyGrnPendingCount()
  const poNeedAction = (poStats?.pendingRepApproval ?? 0) + (poStats?.pendingManagerApproval ?? 0) + (poStats?.pendingAcknowledgement ?? 0)

  return (
    <div className="flex flex-col gap-2.5">
      <ActionPanel icon={ReceiptText} num="01" title="Bills Pending Approval" count={pendingCount} isLoading={pendingLoading}
        description="Review and approve submitted bills from your reps" href="/distributor-billings"
        accentClass="bg-amber-50 text-amber-600 dark:bg-amber-950/40 dark:text-amber-400" />
      <ActionPanel icon={ShoppingCart} num="02" title="Purchase Orders In Pipeline" count={poNeedAction} isLoading={poLoading}
        description="Orders pending rep/manager approval or your acknowledgement" href="/distributor-purchase-orders"
        accentClass="bg-blue-50 text-blue-600 dark:bg-blue-950/40 dark:text-blue-400" />
      <ActionPanel icon={FileText} num="03" title="GRNs to Confirm" count={grnPending} isLoading={grnLoading}
        description="Goods received notes awaiting your confirmation" href="/distributor-grns"
        accentClass="bg-violet-50 text-violet-600 dark:bg-violet-950/40 dark:text-violet-400" />
    </div>
  )
}

// ── Section 5: Stock Health ───────────────────────────────────────────────

function StockHealth() {
  const { data, isLoading } = useMyStockSummary()

  if (isLoading) {
    return (
      <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
        <Skeleton className="h-[180px] rounded-xl" />
        <Skeleton className="h-[180px] rounded-xl" />
      </div>
    )
  }

  const s = data ?? { totalSkus: 0, totalQoH: 0, normalCount: 0, freeIssueCount: 0, lowStockItems: [] }
  const maxQoH = s.lowStockItems.length > 0 ? Math.max(...s.lowStockItems.map(i => i.quantityOnHand), 1) : 1

  return (
    <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
      <div className="rounded-xl border bg-card shadow-sm overflow-hidden">
        <div className="flex items-center gap-2 px-5 py-3.5 border-b">
          <Boxes className="h-3.5 w-3.5 text-muted-foreground" />
          <p className="text-xs font-semibold">Inventory Overview</p>
        </div>
        <div className="grid grid-cols-2 divide-x divide-y">
          <div className="px-5 py-4">
            <p className="text-[10px] text-muted-foreground uppercase tracking-wide">Total SKUs</p>
            <p className="text-2xl font-bold tabular-nums mt-1">{s.totalSkus}</p>
          </div>
          <div className="px-5 py-4">
            <p className="text-[10px] text-muted-foreground uppercase tracking-wide">Total QoH</p>
            <p className="text-2xl font-bold tabular-nums mt-1">{s.totalQoH.toLocaleString()}</p>
          </div>
          <div className="px-5 py-4">
            <p className="text-[10px] text-emerald-700 dark:text-emerald-400 uppercase tracking-wide">Normal</p>
            <p className="text-xl font-bold tabular-nums text-emerald-700 dark:text-emerald-400 mt-1">{s.normalCount}</p>
          </div>
          <div className="px-5 py-4">
            <p className="text-[10px] text-blue-600 dark:text-blue-400 uppercase tracking-wide">Free Issue</p>
            <p className="text-xl font-bold tabular-nums text-blue-600 dark:text-blue-400 mt-1">{s.freeIssueCount}</p>
          </div>
        </div>
      </div>

      <div className="rounded-xl border bg-card shadow-sm overflow-hidden">
        <div className="flex items-center gap-2 px-5 py-3.5 border-b">
          <Activity className="h-3.5 w-3.5 text-amber-500" />
          <p className="text-xs font-semibold">Lowest Stock</p>
        </div>
        <div className="px-5 py-3 flex flex-col gap-3">
          {s.lowStockItems.length === 0 ? (
            <p className="text-xs text-muted-foreground py-4 text-center">No stock items found</p>
          ) : (
            s.lowStockItems.map((item) => (
              <div key={item.productCode} className="flex flex-col gap-1">
                <div className="flex items-center justify-between gap-2">
                  <div className="min-w-0 flex-1">
                    <span className="text-[9px] font-mono text-muted-foreground">{item.productCode}</span>
                    <p className="text-xs font-medium truncate leading-tight">{item.productDescription}</p>
                  </div>
                  <span className={`shrink-0 text-sm font-bold tabular-nums ${
                    item.quantityOnHand === 0 ? "text-red-500" :
                    item.quantityOnHand < 10 ? "text-amber-600 dark:text-amber-400" : "text-foreground"
                  }`}>{item.quantityOnHand}</span>
                </div>
                <div className="h-1 rounded-full bg-muted overflow-hidden">
                  <div
                    className={`h-full rounded-full ${item.quantityOnHand === 0 ? "bg-red-500" : item.quantityOnHand < 10 ? "bg-amber-400" : "bg-emerald-500"}`}
                    style={{ width: `${Math.max(2, (item.quantityOnHand / maxQoH) * 100)}%` }}
                  />
                </div>
              </div>
            ))
          )}
        </div>
      </div>
    </div>
  )
}

// ── Section 6: PO Pipeline — circle-node visualization ────────────────────

const PO_STAGES = [
  { key: "pendingRepApproval" as const, label: "Rep Approval", short: "Rep" },
  { key: "pendingManagerApproval" as const, label: "Mgr Approval", short: "Mgr" },
  { key: "pendingAcknowledgement" as const, label: "Acknowledgement", short: "Ack" },
  { key: "finalized" as const, label: "Finalized", short: "Done" },
]

function PoPipeline() {
  const { data: stats, isLoading } = useMyPurchaseOrderStats()

  return (
    <div className="rounded-xl border bg-card shadow-sm overflow-hidden">
      <div className="flex items-center justify-between px-6 py-4 border-b">
        <div>
          <p className="text-sm font-semibold leading-none">Purchase Order Pipeline</p>
          <p className="text-[11px] text-muted-foreground mt-1">
            {isLoading ? "Loading…" : `${stats?.total ?? 0} total orders`}
          </p>
        </div>
        <Link href="/distributor-purchase-orders" className="flex items-center gap-1 text-[11px] text-primary hover:underline">
          View all <ArrowUpRight className="h-3 w-3" />
        </Link>
      </div>
      <div className="px-6 py-6 flex items-center">
        {PO_STAGES.map((stage, idx) => {
          const count = stats?.[stage.key] ?? 0
          const hasItems = count > 0
          return (
            <div key={stage.key} className="flex items-center flex-1 min-w-0">
              <div className="flex flex-col items-center gap-2 flex-1">
                <div className={`flex h-12 w-12 items-center justify-center rounded-full border-2 transition-all ${
                  isLoading ? "border-muted bg-muted/30" :
                  hasItems ? "border-primary bg-primary/10" : "border-border bg-muted/20"
                }`}>
                  {isLoading
                    ? <Skeleton className="h-5 w-5 rounded-full" />
                    : <span className={`text-base font-bold tabular-nums leading-none ${hasItems ? "text-primary" : "text-muted-foreground/40"}`}>{count}</span>
                  }
                </div>
                <div className="text-center px-1">
                  <p className={`text-[9px] font-semibold uppercase tracking-wide hidden sm:block ${hasItems ? "text-primary" : "text-muted-foreground/50"}`}>{stage.label}</p>
                  <p className={`text-[9px] font-semibold uppercase tracking-wide sm:hidden ${hasItems ? "text-primary" : "text-muted-foreground/50"}`}>{stage.short}</p>
                </div>
              </div>
              {idx < PO_STAGES.length - 1 && (
                <div className={`h-px w-full max-w-[48px] mx-1 shrink ${(stats?.[stage.key] ?? 0) > 0 ? "bg-primary/30" : "bg-border"}`} />
              )}
            </div>
          )
        })}
      </div>
    </div>
  )
}

// ── Section 7: Quick links ────────────────────────────────────────────────

const quickLinks = [
  { title: "Purchase Orders", description: "View and track orders", icon: ShoppingCart, href: "/distributor-purchase-orders" },
  { title: "Stock", description: "Current inventory levels", icon: Package, href: "/distributor-stock" },
  { title: "GRNs", description: "Goods received notes", icon: FileText, href: "/distributor-grns" },
  { title: "Sales Invoices", description: "Invoices & payments", icon: BarChart3, href: "/distributor-sales-invoices" },
  { title: "Bills", description: "Rep billing transactions", icon: ReceiptText, href: "/distributor-billings" },
]

function QuickLinkCard({ title, description, icon: Icon, href }: (typeof quickLinks)[number]) {
  return (
    <Link href={href} className="group flex items-center gap-3 rounded-xl border bg-card px-4 py-3.5 shadow-sm transition-all hover:border-primary/40 hover:bg-primary/[0.02] hover:shadow-md">
      <div className="flex h-9 w-9 shrink-0 items-center justify-center rounded-lg bg-muted group-hover:bg-primary/10 transition-colors">
        <Icon className="h-4 w-4 text-muted-foreground group-hover:text-primary transition-colors" />
      </div>
      <div className="flex-1 min-w-0">
        <p className="text-sm font-semibold leading-none group-hover:text-primary transition-colors">{title}</p>
        <p className="mt-0.5 text-[11px] text-muted-foreground truncate">{description}</p>
      </div>
      <ArrowUpRight className="h-3.5 w-3.5 shrink-0 text-muted-foreground/25 group-hover:text-primary transition-colors" />
    </Link>
  )
}

// ── Page ──────────────────────────────────────────────────────────────────

export default function DistributorDashboardPage() {
  const { data: session } = useSession()
  const { data: profile } = useMyDistributorProfile()
  const firstName = session?.user?.name?.split(" ")[0]
  const category = profile?.category

  return (
    <div className="flex flex-1 flex-col gap-6 p-6">

      {/* Header */}
      <div className="relative flex items-center justify-between bg-muted/90 p-10 rounded-lg overflow-hidden">
        <div className="pointer-events-none absolute inset-0 opacity-[0.04]"
          style={{ backgroundImage: "radial-gradient(circle, currentColor 1px, transparent 1px)", backgroundSize: "22px 22px" }} />
        <div className="relative z-10">
          <p className="text-[10px] font-semibold uppercase tracking-[0.25em] text-muted-foreground mb-2">Distributor Portal</p>
          <div className="flex items-center gap-3 flex-wrap">
            <h1 className="text-3xl font-bold tracking-tight">
              {firstName ? `Welcome back, ${firstName}` : "Welcome back"}
            </h1>
            {category && (
              <span className={`rounded-full px-2.5 py-0.5 text-[10px] font-bold tracking-widest ${CATEGORY_STYLES[category] ?? CATEGORY_STYLES.D}`}>
                CAT {category}
              </span>
            )}
          </div>
          <p className="text-muted-foreground mt-1.5 text-sm">{formatDate(new Date())}</p>
        </div>
      </div>

      <section>
        <SectionHeading>Today&apos;s Pulse</SectionHeading>
        <TodaysPulse />
      </section>

      <section>
        <SectionHeading>This Week&apos;s Billing</SectionHeading>
        <WeeklyTrend />
      </section>

      <div className="grid grid-cols-1 gap-6 lg:grid-cols-2">
        <section>
          <SectionHeading>Action Required</SectionHeading>
          <ActionRequired />
        </section>
        <section>
          <SectionHeading>Stock Health</SectionHeading>
          <StockHealth />
        </section>
      </div>

      <section>
        <SectionHeading>Purchase Order Pipeline</SectionHeading>
        <PoPipeline />
      </section>

      <section>
        <SectionHeading>Quick Access</SectionHeading>
        <div className="grid grid-cols-1 gap-2 sm:grid-cols-2 lg:grid-cols-5">
          {quickLinks.map((link) => <QuickLinkCard key={link.href} {...link} />)}
        </div>
      </section>

    </div>
  )
}
