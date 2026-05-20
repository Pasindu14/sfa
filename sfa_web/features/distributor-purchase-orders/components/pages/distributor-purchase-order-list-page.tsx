'use client'

import { Bell, Clock, TrendingUp, Package } from 'lucide-react'
import { Card, CardContent } from '@/components/ui/card'
import { Skeleton } from '@/components/ui/skeleton'
import { DistributorPurchaseOrderTable } from '../table/distributor-purchase-order-table'
import { useMyPurchaseOrderStats } from '../../hooks/distributor-purchase-order.hooks'

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
          <div className="flex flex-col gap-1">
            <p className="text-xs font-semibold uppercase tracking-wide text-muted-foreground">
              {title}
            </p>
            {loading ? (
              <Skeleton className="h-8 w-16" />
            ) : (
              <p className={`text-3xl font-bold ${valueClassName ?? 'text-foreground'}`}>
                {value ?? 0}
              </p>
            )}
            <p className="text-xs text-muted-foreground">{subtitle}</p>
          </div>
          <div className="rounded-md bg-muted p-2">{icon}</div>
        </div>
      </CardContent>
    </Card>
  )
}

export function DistributorPurchaseOrderListPage() {
  const { data: stats, isLoading: statsLoading } = useMyPurchaseOrderStats()

  return (
    <div className="flex flex-col gap-6 p-6">
      {/* Header */}
      <div className="bg-muted/90 p-10 rounded-lg">
        <h1 className="text-3xl font-bold tracking-tight">My Purchase Orders</h1>
        <p className="text-muted-foreground">Create and track your purchase orders</p>
      </div>

      {/* KPI Stats */}
      <div className="grid grid-cols-2 gap-4 lg:grid-cols-4">
        <StatCard
          title="In Review"
          value={stats ? (stats.pendingRepApproval + stats.pendingManagerApproval) : undefined}
          subtitle="Awaiting internal approval"
          loading={statsLoading}
          icon={<Clock className="h-4 w-4 text-muted-foreground" />}
        />
        <StatCard
          title="Total Orders"
          value={stats?.total}
          subtitle="All time orders"
          loading={statsLoading}
          icon={<Package className="h-4 w-4 text-muted-foreground" />}
        />
        <StatCard
          title="Pending Acknowledgement"
          value={stats?.pendingAcknowledgement}
          subtitle="Rejected — action required"
          loading={statsLoading}
          valueClassName="text-orange-500"
          icon={<Bell className="h-4 w-4 text-orange-400" />}
        />
        <StatCard
          title="Finalized"
          value={stats?.finalized}
          subtitle="Successfully completed"
          loading={statsLoading}
          valueClassName="text-green-600"
          icon={<TrendingUp className="h-4 w-4 text-green-500" />}
        />
      </div>

      {/* Table */}
      <DistributorPurchaseOrderTable />
    </div>
  )
}
