import { SalesOrderEditPage } from '@/features/sales-order/components/pages/sales-order-edit-page'

interface Props {
  params: Promise<{ id: string }>
}

export default async function SalesOrderEditRoute({ params }: Props) {
  const { id } = await params
  return <SalesOrderEditPage orderId={Number(id)} />
}
