import { OutletBillsPage } from '@/features/outlet/components/pages/outlet-bills-page'

interface Props {
  params: Promise<{ id: string }>
}

export default async function PortalOutletBillsRoute({ params }: Props) {
  await params
  return <OutletBillsPage />
}
