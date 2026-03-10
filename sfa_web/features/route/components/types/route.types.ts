export type { RouteDto } from '../../schema/route.schema'

export type RouteTableMeta = {
  onEdit: (id: number) => void
  onDelete: (id: number) => void
}
