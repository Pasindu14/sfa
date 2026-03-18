'use client'

import dynamic from 'next/dynamic'

const PurchaseOrderListPage = dynamic(
  () =>
    import('@/features/purchase-order/components/pages/purchase-order-list-page').then((m) => ({
      default: m.PurchaseOrderListPage,
    })),
  { ssr: false }
)

export default function PurchaseOrdersPage() {
  return <PurchaseOrderListPage />
}
