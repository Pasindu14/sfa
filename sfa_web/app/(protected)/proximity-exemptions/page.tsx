'use client'

import dynamic from 'next/dynamic'
import { ErrorBoundary } from '@/components/error-boundary'
import { ErrorState } from '@/components/error-state'

const ProximityExemptionListPage = dynamic(
  () =>
    import(
      '@/features/proximity-exemption/components/pages/proximity-exemption-list-page'
    ).then((m) => ({ default: m.ProximityExemptionListPage })),
  { ssr: false }
)

export default function ProximityExemptionsPage() {
  return (
    <ErrorBoundary fallback={<ErrorState />}>
      <ProximityExemptionListPage />
    </ErrorBoundary>
  )
}
