import { notFound } from 'next/navigation'
import { SalesOrderDetailPage } from '@/features/sales-order/components/pages/sales-order-detail-page'

// Next.js 15+: params is a Promise — must be async and awaited
interface DetailPageProps {
  params: Promise<{ id: string }>
}

export default async function SalesOrderDetailRoute({ params }: DetailPageProps) {
  const { id } = await params
  const numId = parseInt(id, 10)
  if (isNaN(numId)) notFound()
  return <SalesOrderDetailPage orderId={numId} />
}
