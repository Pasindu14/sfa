export type { PricingStructureDto, PricingStructureItemDto } from '../../schema/pricing-structure.schema'

export type PricingStructureTableMeta = {
  onEdit: (id: number) => void
  onDelete: (id: number) => void
  onActivate: (id: number) => void
  onManageItems: (id: number) => void
}
