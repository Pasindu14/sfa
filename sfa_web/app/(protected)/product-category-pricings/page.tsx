'use client'

import dynamic from 'next/dynamic'

const ProductCategoryPricingPage = dynamic(
  () =>
    import('@/features/product-category-pricing/components').then((m) => ({
      default: m.ProductCategoryPricingPage,
    })),
  { ssr: false }
)

export default function ProductCategoryPricingsPage() {
  return <ProductCategoryPricingPage />
}
