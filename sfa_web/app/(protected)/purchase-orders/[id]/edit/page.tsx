import { PurchaseOrderEditPage } from '@/features/purchase-order/components/pages/purchase-order-edit-page'

interface Props {
  params: Promise<{ id: string }>
}

export default async function PurchaseOrderEditRoute({ params }: Props) {
  const { id } = await params
  return <PurchaseOrderEditPage orderId={Number(id)} />
}
