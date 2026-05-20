'use client'

import dynamic from 'next/dynamic'
import { use } from 'react'
import { ErrorBoundary } from '@/components/error-boundary'
import { ErrorState } from '@/components/error-state'

const DistributorPurchaseOrderEditPage = dynamic(
  () =>
    import('@/features/distributor-purchase-orders/components').then((m) => ({
      default: m.DistributorPurchaseOrderEditPage,
    })),
  { ssr: false },
)

interface Props {
  params: Promise<{ id: string }>
}

export default function DistributorPurchaseOrderEditRoute({ params }: Props) {
  const { id } = use(params)
  return (
    <ErrorBoundary fallback={<ErrorState />}>
      <DistributorPurchaseOrderEditPage id={Number(id)} />
    </ErrorBoundary>
  )
}
