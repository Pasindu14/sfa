'use client'

import dynamic from 'next/dynamic'
import { ErrorBoundary } from '@/components/error-boundary'
import { ErrorState } from '@/components/error-state'

const ProductListPage = dynamic(
  () =>
    import('@/features/product/components').then((m) => ({
      default: m.ProductListPage,
    })),
  { ssr: false }
)

export default function ProductsPage() {
  return (
    <ErrorBoundary fallback={<ErrorState />}>
      <ProductListPage />
    </ErrorBoundary>
  )
}
