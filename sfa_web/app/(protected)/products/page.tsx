'use client'

import dynamic from 'next/dynamic'

const ProductListPage = dynamic(
  () =>
    import('@/features/product/components').then((m) => ({
      default: m.ProductListPage,
    })),
  { ssr: false }
)

export default function ProductsPage() {
  return <ProductListPage />
}
