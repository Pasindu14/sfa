'use client'

import dynamic from 'next/dynamic'
import { ErrorBoundary } from '@/components/error-boundary'
import { ErrorState } from '@/components/error-state'

const PurchaseOrderListPage = dynamic(
  () =>
    import('@/features/purchase-order/components/pages/purchase-order-list-page').then((m) => ({
      default: m.PurchaseOrderListPage,
    })),
  { ssr: false }
)

export default function PurchaseOrdersPage() {
  return (
    <ErrorBoundary fallback={<ErrorState />}>
      <PurchaseOrderListPage />
    </ErrorBoundary>
  )
}
