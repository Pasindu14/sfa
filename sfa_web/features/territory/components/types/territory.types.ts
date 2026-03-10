export type { TerritoryDto } from '../../schema/territory.schema'

export type TerritoryTableMeta = {
  onEdit: (id: number) => void
  onActivate: (id: number) => void
  onDeactivate: (id: number) => void
}
