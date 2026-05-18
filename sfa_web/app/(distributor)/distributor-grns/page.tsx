'use client'

import dynamic from 'next/dynamic'
import { ErrorBoundary } from '@/components/error-boundary'
import { ErrorState } from '@/components/error-state'

const DistributorGrnPage = dynamic(
  () =>
    import('@/features/distributor-grn/components').then((m) => ({
      default: m.DistributorGrnPage,
    })),
  { ssr: false },
)

export default function DistributorGrnsRoute() {
  return (
    <ErrorBoundary fallback={<ErrorState />}>
      <DistributorGrnPage />
    </ErrorBoundary>
  )
}
