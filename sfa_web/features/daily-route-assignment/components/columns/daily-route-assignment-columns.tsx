'use client'

import type { ColumnDef } from '@tanstack/react-table'
import { MoreHorizontal } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'
import type { DailyRouteAssignmentDto } from '../../schema/daily-route-assignment.schema'
import { formatColombo } from '@/lib/utils/datetime'

function getInitials(name: string) {
  return name
    .split(' ')
    .slice(0, 2)
    .map((n) => n[0])
    .join('')
    .toUpperCase()
}

const deletionStatusBadgeClass: Record<string, string> = {
  PendingApproval: 'bg-amber-100 text-amber-700 border-amber-200',
  Approved: 'bg-muted text-muted-foreground border-border',
  Rejected: 'bg-red-100 text-red-700 border-red-200',
}

export interface DailyRouteAssignmentColumnActions {
  openDelete: (id: number) => void
}

export function getDailyRouteAssignmentColumns(
  actions: DailyRouteAssignmentColumnActions,
): ColumnDef<DailyRouteAssignmentDto>[] {
  const { openDelete } = actions

  return [
    {
      id: 'salesRep',
      header: 'Sales Rep',
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
      accessorKey: 'routeName',
      header: 'Route',
      cell: ({ row }) => <span className="text-sm">{row.original.routeName}</span>,
    },
    {
      accessorKey: 'assignedDate',
      header: 'Assigned Date',
      cell: ({ row }) => (
        <span className="text-sm text-muted-foreground">
          {formatColombo(row.original.assignedDate, 'd MMM yyyy')}
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
      id: 'deletionStatus',
      header: 'Deletion Status',
      cell: ({ row }) => {
        const status = row.original.deletionStatus
        if (status === 'None') return null
        return (
          <Badge
            variant="outline"
            className={`text-xs font-medium ${deletionStatusBadgeClass[status] ?? ''}`}
          >
            {status === 'PendingApproval' ? 'Pending Approval' : status}
          </Badge>
        )
      },
    },
    {
      id: 'actions',
      size: 70,
      header: 'Actions',
      cell: ({ row }) => {
        const assignment = row.original
        return (
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button variant="ghost" size="icon" className="h-8 w-8">
                <MoreHorizontal className="h-4 w-4" />
                <span className="sr-only">Open menu</span>
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end">
              <DropdownMenuItem
                onClick={() => openDelete(assignment.id)}
                className="text-destructive focus:text-destructive"
              >
                Remove
              </DropdownMenuItem>
            </DropdownMenuContent>
          </DropdownMenu>
        )
      },
    },
  ]
}
