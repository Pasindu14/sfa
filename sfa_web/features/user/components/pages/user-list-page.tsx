'use client'

import { UserTable } from '../table/user-table'
import { UserDialogs } from '../dialogs/user-dialogs'

export function UserListPage() {
  return (
    <div className="flex flex-col gap-6 p-6">
 <div className="flex items-center justify-between bg-muted/90 p-10 rounded-lg">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">User Management</h1>
          <p className="text-muted-foreground">Manage your user records</p>
        </div>
      </div>

      <UserTable />
      <UserDialogs />
    </div>
  )
}
