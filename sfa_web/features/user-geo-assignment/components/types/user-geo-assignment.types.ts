export type { UserAssignmentDto } from '../../schema/user-geo-assignment.schema'

export type UserGeoAssignmentTableMeta = {
  onEdit: (id: number) => void
  onDeactivate: (id: number) => void
  onActivate: (id: number) => void
}
