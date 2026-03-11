export type { RouteDto } from '../../schema/route.schema'

export type RouteTableMeta = {
  onEdit: (id: number) => void
  onActivate: (id: number) => void
  onDeactivate: (id: number) => void
}
