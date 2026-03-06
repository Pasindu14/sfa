'use client'

import { UserTable } from '../table/user-table'
import { UserDialogs } from '../dialogs/user-dialogs'

export function UserListPage() {
  return (
    <div className="space-y-4 p-6">
      <div>
        <h1 className="text-2xl font-bold tracking-tight">Users</h1>
        <p className="text-muted-foreground">Manage system users and their access.</p>
      </div>

      <UserTable />
      <UserDialogs />
    </div>
  )
}
