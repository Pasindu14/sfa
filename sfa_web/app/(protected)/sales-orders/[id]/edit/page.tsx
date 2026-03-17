import { SalesOrderEditPage } from '@/features/sales-order/components/pages/sales-order-edit-page'

// Next.js 15+: params is a Promise — must be async and awaited
interface EditPageProps {
  params: Promise<{ id: string }>
}

export default async function EditSalesOrderPage({ params }: EditPageProps) {
  const { id } = await params
  return <SalesOrderEditPage orderId={parseInt(id)} />
}
