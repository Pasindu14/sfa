'use client'

import dynamic from 'next/dynamic'

const SalesOrderCreatePage = dynamic(
  () =>
    import('@/features/sales-order/components/pages/sales-order-create-page').then((m) => ({
      default: m.SalesOrderCreatePage,
    })),
  { ssr: false }
)

export default function NewSalesOrderPage() {
  return <SalesOrderCreatePage />
}
