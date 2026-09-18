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
            Named price lists for your products. Reps bill from active structures; the default
            one is preselected on new bills and prices back-office purchase orders.
          </p>
        </div>
      </div>

      <PricingStructureTable />
      <PricingStructureDialogs />
    </div>
  )
}
