"use client"

import Link from "next/link"
import { useSession } from "next-auth/react"
import {
  ShoppingCart, Package, FileText, BarChart3,
  ReceiptText, TrendingUp, BadgeCheck, Clock4, ArrowUpRight,
} from "lucide-react"
import { Skeleton } from "@/components/ui/skeleton"
import { useMyBillingsTodaySummary } from "@/features/distributor-billings/hooks/distributor-billing.hooks"

function formatCurrency(amount: number) {
  return new Intl.NumberFormat("en-LK", {
    style: "currency",
    currency: "LKR",
    minimumFractionDigits: 0,
    maximumFractionDigits: 0,
  }).format(amount)
}

function formatDate(d: Date) {
  return d.toLocaleDateString("en-US", {
    weekday: "long",
    day: "numeric",
    month: "long",
    year: "numeric",
  })
}

// ── Billing stat card ─────────────────────────────────────────────────────────

interface StatCardProps {
  icon: React.ElementType
  label: string
  value: React.ReactNode
  sub: string
  iconClass: string
}

function StatCard({ icon: Icon, label, value, sub, iconClass }: StatCardProps) {
  return (
    <div className="flex flex-col gap-3 rounded-xl border bg-card px-5 py-4 shadow-sm">
      <div className="flex items-center justify-between">
        <p className="text-[10px] font-semibold uppercase tracking-[0.15em] text-muted-foreground">
          {label}
        </p>
        <div className={`flex h-7 w-7 items-center justify-center rounded-lg ${iconClass}`}>
          <Icon className="h-3.5 w-3.5" />
        </div>
      </div>
      <div>
        <p className="text-2xl font-bold tabular-nums tracking-tight leading-none">
          {value}
        </p>
        <p className="mt-1.5 text-[11px] text-muted-foreground">{sub}</p>
      </div>
    </div>
  )
}

function BillingStats() {
  const { data, isLoading } = useMyBillingsTodaySummary()

  if (isLoading) {
    return (
      <div className="grid grid-cols-1 gap-3 sm:grid-cols-3">
        {[1, 2, 3].map((i) => (
          <Skeleton key={i} className="h-[100px] rounded-xl" />
        ))}
      </div>
    )
  }

  const s = data ?? { totalRevenue: 0, totalCount: 0, approvedRevenue: 0, approvedCount: 0, submittedCount: 0 }

  return (
    <div className="grid grid-cols-1 gap-3 sm:grid-cols-3">
      <StatCard
        icon={TrendingUp}
        label="Today's Revenue"
        value={formatCurrency(s.totalRevenue)}
        sub={`${s.totalCount} bill${s.totalCount !== 1 ? "s" : ""} issued today`}
        iconClass="bg-emerald-50 text-emerald-600 dark:bg-emerald-950/40 dark:text-emerald-400"
      />
      <StatCard
        icon={BadgeCheck}
        label="Approved"
        value={formatCurrency(s.approvedRevenue)}
        sub={`${s.approvedCount} bill${s.approvedCount !== 1 ? "s" : ""} confirmed`}
        iconClass="bg-blue-50 text-blue-600 dark:bg-blue-950/40 dark:text-blue-400"
      />
      <StatCard
        icon={Clock4}
        label="Pending Review"
        value={s.submittedCount}
        sub={s.submittedCount > 0 ? "Awaiting approval" : "All bills reviewed"}
        iconClass="bg-amber-50 text-amber-600 dark:bg-amber-950/40 dark:text-amber-400"
      />
    </div>
  )
}

// ── Quick link card ───────────────────────────────────────────────────────────

const quickLinks = [
  {
    title: "Purchase Orders",
    description: "View and track orders",
    icon: ShoppingCart,
    href: "/distributor-purchase-orders",
  },
  {
    title: "Stock",
    description: "Current inventory levels",
    icon: Package,
    href: "/distributor-stock",
  },
  {
    title: "GRNs",
    description: "Goods received notes",
    icon: FileText,
    href: "/distributor-grn",
  },
  {
    title: "Sales Invoices",
    description: "Invoices & payments",
    icon: BarChart3,
    href: "/distributor-sales-invoices",
  },
  {
    title: "Bills",
    description: "Rep billing transactions",
    icon: ReceiptText,
    href: "/distributor-billings",
  },
]

function QuickLinkCard({
  title,
  description,
  icon: Icon,
  href,
}: (typeof quickLinks)[number]) {
  return (
    <Link
      href={href}
      className="group flex flex-col gap-3 rounded-xl border bg-card px-4 py-4 shadow-sm transition-all hover:border-primary/40 hover:shadow-md hover:-translate-y-px"
    >
      <div className="flex items-center justify-between">
        <div className="flex h-8 w-8 items-center justify-center rounded-lg bg-muted group-hover:bg-primary/10 transition-colors">
          <Icon className="h-4 w-4 text-muted-foreground group-hover:text-primary transition-colors" />
        </div>
        <ArrowUpRight className="h-3.5 w-3.5 text-muted-foreground/40 group-hover:text-primary transition-colors" />
      </div>
      <div>
        <p className="text-sm font-semibold leading-none group-hover:text-primary transition-colors">
          {title}
        </p>
        <p className="mt-1 text-xs text-muted-foreground">{description}</p>
      </div>
    </Link>
  )
}

// ── Page ──────────────────────────────────────────────────────────────────────

export default function DistributorDashboardPage() {
  const { data: session } = useSession()
  const firstName = session?.user?.name?.split(" ")[0]

  return (
    <div className="flex flex-1 flex-col">

      {/* Welcome banner */}
      <div className="bg-foreground px-6 py-7 text-background">
        <div className="flex items-end justify-between gap-4">
          <div>
            <p className="text-[10px] font-semibold uppercase tracking-[0.2em] text-background/40 mb-1">
              Distributor Portal
            </p>
            <h1 className="text-2xl font-bold tracking-tight">
              {firstName ? `Welcome back, ${firstName}` : "Welcome back"}
            </h1>
          </div>
          <div className="shrink-0 text-right">
            <p className="text-[9px] font-semibold uppercase tracking-[0.2em] text-background/40">
              Today
            </p>
            <p className="text-xs font-medium text-background/70 mt-0.5">
              {formatDate(new Date())}
            </p>
          </div>
        </div>
      </div>

      {/* Content */}
      <div className="flex flex-col gap-7 p-6">

        {/* Today's performance */}
        <section>
          <p className="text-[10px] font-semibold uppercase tracking-[0.18em] text-muted-foreground mb-3">
            Today&apos;s Performance
          </p>
          <BillingStats />
        </section>

        {/* Quick access */}
        <section>
          <p className="text-[10px] font-semibold uppercase tracking-[0.18em] text-muted-foreground mb-3">
            Quick Access
          </p>
          <div className="grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-5">
            {quickLinks.map((link) => (
              <QuickLinkCard key={link.href} {...link} />
            ))}
          </div>
        </section>

      </div>
    </div>
  )
}
