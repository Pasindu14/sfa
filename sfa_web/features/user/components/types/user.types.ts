export type { UserDto } from '../../schema/user.schema'

export type UserTableMeta = {
  onEdit: (id: number) => void
  onDelete: (id: number) => void
  onChangePassword: (id: number) => void
  onActivate: (id: number) => void
  onDeactivate: (id: number) => void
}
