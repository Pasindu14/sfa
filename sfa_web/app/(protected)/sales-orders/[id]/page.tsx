import { SalesOrderDetailPage } from '@/features/sales-order/components/pages/sales-order-detail-page'

interface Props {
  params: Promise<{ id: string }>
}

export default async function SalesOrderDetailRoute({ params }: Props) {
  const { id } = await params
  return <SalesOrderDetailPage orderId={Number(id)} />
}
