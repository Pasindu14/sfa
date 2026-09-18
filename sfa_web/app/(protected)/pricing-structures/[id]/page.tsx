'use client'

import { use } from 'react'
import dynamic from 'next/dynamic'
import { ErrorBoundary } from '@/components/error-boundary'
import { ErrorState } from '@/components/error-state'

const PricingStructurePricesPage = dynamic(
  () =>
    import('@/features/pricing-structure/components').then((m) => ({
      default: m.PricingStructurePricesPage,
    })),
  { ssr: false }
)

interface Props {
  params: Promise<{ id: string }>
}

export default function PricingStructurePricesRoute({ params }: Props) {
  const { id } = use(params)
  return (
    <ErrorBoundary fallback={<ErrorState />}>
      <PricingStructurePricesPage structureId={Number(id)} />
    </ErrorBoundary>
  )
}
