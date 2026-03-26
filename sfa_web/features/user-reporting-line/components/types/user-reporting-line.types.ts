export type { UserReportingLineDto } from '../../schema/user-reporting-line.schema'

export type UserReportingLineTableMeta = {
  onEdit: (id: number) => void
  onDelete: (id: number) => void
}
