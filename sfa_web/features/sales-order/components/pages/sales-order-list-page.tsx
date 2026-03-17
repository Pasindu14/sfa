'use client'

import Link from 'next/link'
import { Plus } from 'lucide-react'
import { useSession } from 'next-auth/react'
import { Button } from '@/components/ui/button'
import { SalesOrderTable } from '../table/sales-order-table'

export function SalesOrderListPage() {
  const { data: session } = useSession()
  const role = session?.user?.role ?? ''
  const canCreate = role === 'Admin' || role === 'Distributor'

  return (
    <div className="flex flex-col gap-6 p-6">
      <div className="flex items-center justify-between bg-muted/90 p-10 rounded-lg">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Sales Orders</h1>
          <p className="text-muted-foreground">Manage and track your sales orders</p>
        </div>
        {canCreate && (
          <Button asChild className="gap-2">
            <Link href="/sales-orders/new">
              <Plus className="h-4 w-4" />
              Create Sales Order
            </Link>
          </Button>
        )}
      </div>
      <SalesOrderTable />
    </div>
  )
}
