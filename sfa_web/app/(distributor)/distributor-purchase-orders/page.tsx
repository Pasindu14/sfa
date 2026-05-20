'use client'

import dynamic from 'next/dynamic'
import { ErrorBoundary } from '@/components/error-boundary'
import { ErrorState } from '@/components/error-state'

const DistributorPurchaseOrderListPage = dynamic(
  () =>
    import('@/features/distributor-purchase-orders/components').then((m) => ({
      default: m.DistributorPurchaseOrderListPage,
    })),
  { ssr: false },
)

export default function DistributorPurchaseOrdersRoute() {
  return (
    <ErrorBoundary fallback={<ErrorState />}>
      <DistributorPurchaseOrderListPage />
    </ErrorBoundary>
  )
}
