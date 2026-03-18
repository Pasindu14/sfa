'use client'

import dynamic from 'next/dynamic'

const PurchaseOrderCreatePage = dynamic(
  () =>
    import('@/features/purchase-order/components/pages/purchase-order-create-page').then((m) => ({
      default: m.PurchaseOrderCreatePage,
    })),
  { ssr: false }
)

export default function NewPurchaseOrderPage() {
  return <PurchaseOrderCreatePage />
}
