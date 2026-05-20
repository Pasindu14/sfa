'use client'

import dynamic from 'next/dynamic'
import { use } from 'react'
import { ErrorBoundary } from '@/components/error-boundary'
import { ErrorState } from '@/components/error-state'

const DistributorPurchaseOrderDetailPage = dynamic(
  () =>
    import('@/features/distributor-purchase-orders/components').then((m) => ({
      default: m.DistributorPurchaseOrderDetailPage,
    })),
  { ssr: false },
)

interface Props {
  params: Promise<{ id: string }>
}

export default function DistributorPurchaseOrderDetailRoute({ params }: Props) {
  const { id } = use(params)
  return (
    <ErrorBoundary fallback={<ErrorState />}>
      <DistributorPurchaseOrderDetailPage id={Number(id)} />
    </ErrorBoundary>
  )
}
