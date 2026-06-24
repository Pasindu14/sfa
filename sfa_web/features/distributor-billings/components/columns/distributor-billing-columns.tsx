'use client'

import type { ColumnDef } from '@tanstack/react-table'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Eye, ClipboardCheck, Banknote } from 'lucide-react'
import type { DistributorBillingListItem } from '../../schema/distributor-billing.schema'
import { formatColombo } from '@/lib/utils/datetime'

function formatCurrency(amount: number) {
  return new Intl.NumberFormat('en-LK', {
    style: 'currency',
    currency: 'LKR',
    minimumFractionDigits: 2,
  }).format(amount)
}

function RepStatusBadge({ status }: { status: DistributorBillingListItem['repStatus'] }) {
  if (status === 'Cancelled')
    return <Badge variant="destructive" className="text-xs">Cancelled</Badge>
  return <Badge variant="outline" className="text-xs">Submitted</Badge>
}

function DistributorStatusBadge({ status }: { status: DistributorBillingListItem['distributorStatus'] }) {
  if (status === 'Approved')
    return <Badge className="bg-green-600 hover:bg-green-700 text-white text-xs">Approved</Badge>
  if (status === 'Rejected')
    return <Badge className="bg-amber-500 hover:bg-amber-600 text-white text-xs">Rejected</Badge>
  return <Badge variant="secondary" className="text-xs">Pending</Badge>
}

export function PaymentTypeBadge({ type }: { type: 'Cash' | 'Credit' }) {
  if (type === 'Credit')
    return <Badge className="bg-blue-100 text-blue-700 hover:bg-blue-100 text-xs border-0">Credit</Badge>
  return <Badge variant="outline" className="text-xs">Cash</Badge>
}

export function getDistributorBillingColumns(
  onView: (id: number) => void,
  onReview: (billing: DistributorBillingListItem) => void,
  onCashCollected: (billing: DistributorBillingListItem) => void,
): ColumnDef<DistributorBillingListItem>[] {
  return [
    {
      accessorKey: 'billingNumber',
      header: 'Billing',
      cell: ({ row }) => (
        <div>
          <p className="font-mono text-xs font-semibold">{row.original.billingNumber}</p>
          <p className="text-xs text-muted-foreground">
            {formatColombo(row.original.billingDate, 'd MMM yyyy')}
          </p>
        </div>
      ),
    },
    {
      accessorKey: 'outletName',
      header: 'Outlet',
      cell: ({ row }) => (
        <span className="text-sm">{row.original.outletName}</span>
      ),
    },
    {
      accessorKey: 'salesRepName',
      header: 'Sales Rep',
      cell: ({ row }) => (
        <div className="flex flex-col gap-1">
          <div className="flex items-center gap-1.5">
            <span className="shrink-0 rounded px-1 py-px text-[10px] font-bold uppercase tracking-wider bg-primary/10 text-primary leading-none">
              Rep
            </span>
            <span className="text-sm font-medium leading-none">{row.original.salesRepName}</span>
          </div>
          {row.original.supervisorName && (
            <div className="flex items-center gap-1.5 pl-px">
              <span className="shrink-0 rounded px-1 py-px text-[10px] font-bold uppercase tracking-wider bg-muted text-muted-foreground leading-none">
                Sup
              </span>
              <span className="text-xs text-muted-foreground leading-none">{row.original.supervisorName}</span>
            </div>
          )}
        </div>
      ),
    },
    {
      accessorKey: 'totalAmount',
      header: () => <span className="block text-right">Amount</span>,
      cell: ({ row }) => (
        <span className="block text-right tabular-nums text-sm font-semibold">
          {formatCurrency(row.original.totalAmount)}
        </span>
      ),
    },
    {
      id: 'paymentType',
      header: 'Payment',
      cell: ({ row }) => <PaymentTypeBadge type={row.original.paymentType} />,
    },
    {
      id: 'cashCollected',
      header: 'Cash',
      cell: ({ row }) => {
        if (row.original.isCashCollected)
          return <Badge className="bg-green-100 text-green-700 hover:bg-green-100 text-xs border-0">Collected</Badge>
        return <Badge variant="secondary" className="text-xs text-muted-foreground">Pending</Badge>
      },
    },
    {
      id: 'billingDate',
      header: 'Billed Date',
      cell: ({ row }) => (
        <span className="text-sm tabular-nums">
          {formatColombo(row.original.billingDate, 'd MMM yyyy')}
        </span>
      ),
    },
    {
      id: 'distributorStatus',
      header: 'Distributor Status',
      cell: ({ row }) => <DistributorStatusBadge status={row.original.distributorStatus} />,
    },
    {
      id: 'actions',
      header: '',
      size: 210,
      cell: ({ row }) => {
        const isPending =
          row.original.repStatus === 'Submitted' && row.original.distributorStatus === 'Pending'
        return (
          <div className="flex items-center gap-1">
            <Button
              variant="ghost"
              size="sm"
              className="h-7 gap-1.5 text-xs"
              onClick={() => onView(row.original.id)}
            >
              <Eye className="h-3.5 w-3.5" />
              View
            </Button>
            <Button
              variant="outline"
              size="sm"
              className="h-7 gap-1.5 text-xs"
              onClick={() => onCashCollected(row.original)}
            >
              <Banknote className="h-3.5 w-3.5" />
              Cash
            </Button>
            {isPending && (
              <Button
                size="sm"
                className="h-7 gap-1.5 text-xs bg-green-600 hover:bg-green-700 text-white"
                onClick={() => onReview(row.original)}
              >
                <ClipboardCheck className="h-3.5 w-3.5" />
                Review
              </Button>
            )}
          </div>
        )
      },
    },
  ]
}
