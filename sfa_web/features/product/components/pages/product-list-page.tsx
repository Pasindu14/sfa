'use client'

import { ProductTable } from '../table/product-table'
import { ProductDialogs } from '../dialogs/product-dialogs'

export function ProductListPage() {
  return (
    <div className="flex flex-col gap-6 p-6">
      <div className="flex items-center justify-between bg-muted/90 p-10 rounded-lg">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Product Management</h1>
          <p className="text-muted-foreground">Manage your product catalogue</p>
        </div>
      </div>

      <ProductTable />
      <ProductDialogs />
    </div>
  )
}
