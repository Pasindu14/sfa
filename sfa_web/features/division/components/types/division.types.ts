export type { DivisionDto } from '../../schema/division.schema'

export type DivisionTableMeta = {
  onEdit: (id: number) => void
  onActivate: (id: number) => void
  onDeactivate: (id: number) => void
}
