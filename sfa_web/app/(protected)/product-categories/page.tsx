'use client'

import dynamic from 'next/dynamic'
import { ErrorBoundary } from '@/components/error-boundary'
import { ErrorState } from '@/components/error-state'

const ProductCategoryListPage = dynamic(
  () =>
    import('@/features/product-category/components').then((m) => ({
      default: m.ProductCategoryListPage,
    })),
  { ssr: false }
)

export default function ProductCategoriesPage() {
  return (
    <ErrorBoundary fallback={<ErrorState />}>
      <ProductCategoryListPage />
    </ErrorBoundary>
  )
}
