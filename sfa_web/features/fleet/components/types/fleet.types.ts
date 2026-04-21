export type { FleetDto } from '../../schema/fleet.schema'

export type FleetTableMeta = {
  onEdit: (id: number) => void
  onActivate: (id: number) => void
  onDeactivate: (id: number) => void
}
