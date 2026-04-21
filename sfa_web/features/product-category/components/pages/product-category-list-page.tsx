'use client'

import { ProductCategoryTable } from '../table/product-category-table'
import { ProductCategoryDialogs } from '../dialogs/product-category-dialogs'

export function ProductCategoryListPage() {
  return (
    <div className="flex flex-col gap-6 p-6">
      <div className="flex items-center justify-between bg-muted/90 p-10 rounded-lg">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Product Categories</h1>
          <p className="text-muted-foreground">Manage product category master records</p>
        </div>
      </div>

      <ProductCategoryTable />
      <ProductCategoryDialogs />
    </div>
  )
}
