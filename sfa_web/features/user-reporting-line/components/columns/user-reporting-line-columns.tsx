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
import type { UserReportingLineDto } from '../types/user-reporting-line.types'

function getInitials(name: string) {
  return name
    .split(' ')
    .slice(0, 2)
    .map((n) => n[0])
    .join('')
    .toUpperCase()
}

const roleBadgeClass: Record<string, string> = {
  NSM: 'bg-blue-100 text-blue-700 border-blue-200',
  RSM: 'bg-purple-100 text-purple-700 border-purple-200',
  ASM: 'bg-indigo-100 text-indigo-700 border-indigo-200',
  Supervisor: 'bg-orange-100 text-orange-700 border-orange-200',
  SalesRep: 'bg-green-100 text-green-700 border-green-200',
  Admin: 'bg-red-100 text-red-700 border-red-200',
  Distributor: 'bg-yellow-100 text-yellow-700 border-yellow-200',
}

function RoleBadge({ role }: { role: string }) {
  const cls = roleBadgeClass[role] ?? 'bg-muted text-muted-foreground border-border'
  return (
    <Badge variant="outline" className={`text-xs font-medium ${cls}`}>
      {role}
    </Badge>
  )
}

export interface UserReportingLineColumnActions {
  openEdit: (id: number) => void
  openDeactivate: (id: number) => void
  openActivate: (id: number) => void
}

export function getUserReportingLineColumns(
  actions: UserReportingLineColumnActions,
): ColumnDef<UserReportingLineDto>[] {
  const { openEdit, openDeactivate, openActivate } = actions

  return [
    {
      id: 'subordinate',
      header: 'Subordinate',
      cell: ({ row }) => {
        const { userName } = row.original
        return (
          <div className="flex items-center gap-3">
            <div className="flex h-8 w-8 shrink-0 items-center justify-center rounded-full bg-muted text-xs font-semibold text-muted-foreground">
              {getInitials(userName)}
            </div>
            <span className="text-sm font-medium">{userName}</span>
          </div>
        )
      },
    },
    {
      id: 'userRole',
      header: 'Role',
      cell: ({ row }) => <RoleBadge role={row.original.userRole} />,
    },
    {
      id: 'reportsTo',
      header: 'Reports To',
      cell: ({ row }) => {
        const { reportsToUserName } = row.original
        return (
          <div className="flex items-center gap-2">
            <div className="flex h-7 w-7 shrink-0 items-center justify-center rounded-full bg-muted text-xs font-semibold text-muted-foreground">
              {getInitials(reportsToUserName)}
            </div>
            <span className="text-sm">{reportsToUserName}</span>
          </div>
        )
      },
    },
    {
      id: 'managerRole',
      header: 'Manager Role',
      cell: ({ row }) => <RoleBadge role={row.original.reportsToUserRole} />,
    },
    {
      accessorKey: 'effectiveFrom',
      header: 'Effective From',
      cell: ({ row }) => (
        <span className="text-sm text-muted-foreground">
          {new Date(row.original.effectiveFrom).toLocaleDateString('en-US', {
            month: 'short',
            day: 'numeric',
            year: 'numeric',
          })}
        </span>
      ),
    },
    {
      accessorKey: 'isActive',
      header: 'Status',
      cell: ({ row }) => (
        <Badge
          variant={row.original.isActive ? 'default' : 'secondary'}
          className="text-xs font-medium"
        >
          {row.original.isActive ? 'Active' : 'Inactive'}
        </Badge>
      ),
    },
    {
      id: 'actions',
      header: 'Actions',
      cell: ({ row }) => {
        const line = row.original
        return (
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button variant="ghost" size="icon" className="h-8 w-8">
                <MoreHorizontal className="h-4 w-4" />
                <span className="sr-only">Open menu</span>
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end">
              <DropdownMenuItem onClick={() => openEdit(line.id)}>
                Edit Reporting Line
              </DropdownMenuItem>
              <DropdownMenuSeparator />
              {line.isActive ? (
                <DropdownMenuItem
                  onClick={() => openDeactivate(line.id)}
                  className="text-destructive focus:text-destructive"
                >
                  Deactivate
                </DropdownMenuItem>
              ) : (
                <DropdownMenuItem onClick={() => openActivate(line.id)}>
                  Activate
                </DropdownMenuItem>
              )}
            </DropdownMenuContent>
          </DropdownMenu>
        )
      },
    },
  ]
}
