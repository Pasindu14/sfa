'use client'

import dynamic from 'next/dynamic'

const SalesOrderListPage = dynamic(
  () =>
    import('@/features/sales-order/components/pages/sales-order-list-page').then((m) => ({
      default: m.SalesOrderListPage,
    })),
  { ssr: false }
)

export default function SalesOrdersPage() {
  return <SalesOrderListPage />
}
