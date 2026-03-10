export type { RegionDto } from '../../schema/region.schema'

export type RegionTableMeta = {
  onEdit: (id: number) => void
  onDelete: (id: number) => void
  onActivate: (id: number) => void
  onDeactivate: (id: number) => void
}
