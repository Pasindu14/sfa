'use client'

import { PricingStructureTable } from '../table/pricing-structure-table'
import { PricingStructureDialogs } from '../dialogs/pricing-structure-dialogs'

export function PricingStructureListPage() {
  return (
    <div className="flex flex-col gap-6 p-6">
      <div className="flex items-center justify-between bg-muted/90 p-10 rounded-lg">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Pricing Structures</h1>
          <p className="text-muted-foreground">
            Manage named price lists used when creating invoices
          </p>
        </div>
      </div>

      <PricingStructureTable />
      <PricingStructureDialogs />
    </div>
  )
}
