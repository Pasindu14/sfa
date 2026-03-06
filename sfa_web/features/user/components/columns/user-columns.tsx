'use client'

import type { ColumnDef } from '@tanstack/react-table'
import { MoreHorizontal } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'
import type { UserDto } from '../types/user.types'

function getInitials(name: string) {
  return name
    .split(' ')
    .slice(0, 2)
    .map((n) => n[0])
    .join('')
    .toUpperCase()
}

const roleLabels: Record<string, string> = {
  Admin: 'Admin',
  Manager: 'Manager',
  SalesRep: 'Sales Rep',
}

export interface UserColumnActions {
  openEdit: (id: number) => void
  openDelete: (id: number) => void
  openChangePassword: (id: number) => void
  openActivate: (id: number) => void
  openDeactivate: (id: number) => void
}

export function getUserColumns(actions: UserColumnActions): ColumnDef<UserDto>[] {
  const { openEdit, openDelete, openChangePassword, openActivate, openDeactivate } = actions

  return [
    {
      id: 'nameEmail',
      header: 'User',
      cell: ({ row }) => {
        const { name, email } = row.original
        const initials = getInitials(name)
        return (
          <div className="flex items-center gap-3">
            <div className="flex h-8 w-8 shrink-0 items-center justify-center rounded-full bg-muted text-xs font-semibold text-muted-foreground">
              {initials}
            </div>
            <div>
              <div className="text-sm font-medium">{name}</div>
              <div className="text-xs text-muted-foreground">{email}</div>
            </div>
          </div>
        )
      },
    },
    {
      accessorKey: 'username',
      header: 'Username',
      cell: ({ row }) => (
        <span className="font-mono text-sm text-muted-foreground">@{row.original.username}</span>
      ),
    },
    {
      accessorKey: 'phone',
      header: 'Phone',
      cell: ({ row }) => <span className="text-sm text-muted-foreground">{row.original.phone}</span>,
    },
    {
      accessorKey: 'role',
      header: 'Role',
      cell: ({ row }) => (
        <Badge variant="outline" className="text-xs font-medium">
          {roleLabels[row.original.role] ?? row.original.role}
        </Badge>
      ),
    },
    {
      accessorKey: 'isActive',
      header: 'Status',
      cell: ({ row }) => (
        <Badge variant={row.original.isActive ? 'default' : 'secondary'} className="text-xs font-medium">
          {row.original.isActive ? 'Active' : 'Inactive'}
        </Badge>
      ),
    },
    {
      accessorKey: 'createdAt',
      header: 'Joined',
      cell: ({ row }) => (
        <span className="text-sm text-muted-foreground">
          {new Date(row.original.createdAt).toLocaleDateString('en-US', {
            month: 'short',
            day: 'numeric',
            year: 'numeric',
          })}
        </span>
      ),
    },
    {
      id: 'actions',
      header: 'Actions',
      cell: ({ row }) => {
        const user = row.original
        return (
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button variant="ghost" size="icon" className="h-8 w-8">
                <MoreHorizontal className="h-4 w-4" />
                <span className="sr-only">Open menu</span>
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end">
              <DropdownMenuItem onClick={() => openEdit(user.id)}>Edit</DropdownMenuItem>
              <DropdownMenuItem onClick={() => openChangePassword(user.id)}>Change Password</DropdownMenuItem>
              {user.isActive ? (
                <DropdownMenuItem onClick={() => openDeactivate(user.id)}>Deactivate</DropdownMenuItem>
              ) : (
                <DropdownMenuItem onClick={() => openActivate(user.id)}>Activate</DropdownMenuItem>
              )}
              <DropdownMenuSeparator />
              <DropdownMenuItem
                onClick={() => openDelete(user.id)}
                className="text-destructive focus:text-destructive"
              >
                Delete
              </DropdownMenuItem>
            </DropdownMenuContent>
          </DropdownMenu>
        )
      },
    },
  ]
}
