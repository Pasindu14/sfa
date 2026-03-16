'use client'

import dynamic from 'next/dynamic'

const PricingStructureListPage = dynamic(
  () =>
    import('@/features/pricing-structure/components').then((m) => ({
      default: m.PricingStructureListPage,
    })),
  { ssr: false }
)

export default function PricingStructuresPage() {
  return <PricingStructureListPage />
}
