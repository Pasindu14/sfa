'use client'

import dynamic from 'next/dynamic'
import { ErrorBoundary } from '@/components/error-boundary'
import { ErrorState } from '@/components/error-state'

const PricingStructureListPage = dynamic(
  () =>
    import('@/features/pricing-structure/components').then((m) => ({
      default: m.PricingStructureListPage,
    })),
  { ssr: false }
)

export default function PricingStructuresPage() {
  return (
    <ErrorBoundary fallback={<ErrorState />}>
      <PricingStructureListPage />
    </ErrorBoundary>
  )
}
