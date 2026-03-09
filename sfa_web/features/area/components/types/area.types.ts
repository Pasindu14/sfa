export type { AreaDto } from '../../schema/area.schema'

export type AreaTableMeta = {
  onEdit: (id: number) => void
  onActivate: (id: number) => void
  onDeactivate: (id: number) => void
}
