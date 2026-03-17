import { SalesOrderDetailPage } from '@/features/sales-order/components/pages/sales-order-detail-page'

interface Props {
  params: Promise<{ id: string }>
}

// Edit page re-uses the detail page for now — the detail page handles edit navigation
export default async function SalesOrderEditRoute({ params }: Props) {
  const { id } = await params
  return <SalesOrderDetailPage orderId={Number(id)} />
}
