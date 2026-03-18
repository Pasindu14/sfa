import { PurchaseOrderDetailPage } from '@/features/purchase-order/components/pages/purchase-order-detail-page'

interface Props {
  params: Promise<{ id: string }>
}

export default async function PurchaseOrderDetailRoute({ params }: Props) {
  const { id } = await params
  return <PurchaseOrderDetailPage orderId={Number(id)} />
}
