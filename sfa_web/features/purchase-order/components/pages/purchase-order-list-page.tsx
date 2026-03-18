'use client'

import { useRouter } from 'next/navigation'
import { Plus, Clock, CheckCircle2, Bell, TrendingUp } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Card, CardContent } from '@/components/ui/card'
import { Skeleton } from '@/components/ui/skeleton'
import { PurchaseOrderTable } from '../table/purchase-order-table'
import { PurchaseOrderDialogs } from '../dialogs/purchase-order-dialogs'
import { usePurchaseOrderStats } from '../../hooks/purchase-order.hooks'
import { usePurchaseOrderFilters } from '../../store'

interface StatCardProps {
  title: string
  value: number | undefined
  subtitle: string
  icon: React.ReactNode
  valueClassName?: string
  loading?: boolean
}

function StatCard({ title, value, subtitle, icon, valueClassName, loading }: StatCardProps) {
  return (
    <Card className="border shadow-sm">
      <CardContent className="p-5">
        <div className="flex items-start justify-between">
          <div className="space-y-1">
            <p className="text-xs font-medium text-muted-foreground uppercase tracking-wide">
              {title}
            </p>
            {loading ? (
              <Skeleton className="h-8 w-12" />
            ) : (
              <p className={`text-3xl font-bold tracking-tight ${valueClassName ?? ''}`}>
                {value ?? 0}
              </p>
            )}
            <p className="text-xs text-muted-foreground">{subtitle}</p>
          </div>
          <div className="mt-0.5 rounded-md p-2 bg-muted/50">{icon}</div>
        </div>
      </CardContent>
    </Card>
  )
}

export function PurchaseOrderListPage() {
  const router = useRouter()
  const { fromDate, toDate } = usePurchaseOrderFilters()
  const { data: stats, isLoading: statsLoading } = usePurchaseOrderStats(fromDate, toDate)

  return (
    <div className="flex flex-col gap-6 p-6">
      {/* Header */}
      <div className="flex items-center justify-between bg-muted/90 p-10 rounded-lg">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Purchase Orders</h1>
          <p className="text-muted-foreground">Manage and track purchase order workflow across all distributors.</p>
        </div>
        <Button onClick={() => router.push('/purchase-orders/new')} className="gap-2">
          <Plus className="h-4 w-4" />
          New Order
        </Button>
      </div>

      {/* KPI Stats */}
      <div className="grid grid-cols-2 gap-4 lg:grid-cols-4">
        <StatCard
          title="Pending Rep Approval"
          value={stats?.pendingRepApproval}
          subtitle="Awaiting rep review"
          loading={statsLoading}
          valueClassName="text-foreground"
          icon={<Clock className="h-4 w-4 text-muted-foreground" />}
        />
        <StatCard
          title="Pending Finalization"
          value={stats?.pendingManagerApproval}
          subtitle="Awaiting manager approval"
          loading={statsLoading}
          valueClassName="text-foreground"
          icon={<TrendingUp className="h-4 w-4 text-muted-foreground" />}
        />
        <StatCard
          title="Pending Acknowledgement"
          value={stats?.pendingAcknowledgement}
          subtitle="Distributor not acknowledged"
          loading={statsLoading}
          valueClassName="text-orange-500"
          icon={<Bell className="h-4 w-4 text-orange-400" />}
        />
        <StatCard
          title="Finalized"
          value={stats?.finalized}
          subtitle="Successfully this period"
          loading={statsLoading}
          valueClassName="text-foreground"
          icon={<CheckCircle2 className="h-4 w-4 text-muted-foreground" />}
        />
      </div>

      {/* Table */}
      <PurchaseOrderTable />
      <PurchaseOrderDialogs />
    </div>
  )
}
