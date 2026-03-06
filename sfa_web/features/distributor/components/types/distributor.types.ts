export type { DistributorDto } from '../../schema/distributor.schema'

export type DistributorTableMeta = {
  onEdit: (id: number) => void
  onDelete: (id: number) => void
  onActivate: (id: number) => void
  onDeactivate: (id: number) => void
}
