'use client'

import dynamic from 'next/dynamic'
import { ErrorBoundary } from '@/components/error-boundary'
import { ErrorState } from '@/components/error-state'

const DistributorBillingPage = dynamic(
  () =>
    import('@/features/distributor-billings/components').then((m) => ({
      default: m.DistributorBillingPage,
    })),
  { ssr: false },
)

export default function DistributorBillingsRoute() {
  return (
    <ErrorBoundary fallback={<ErrorState />}>
      <DistributorBillingPage />
    </ErrorBoundary>
  )
}
