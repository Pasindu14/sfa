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
import type { UserAssignmentDto } from '../types/user-geo-assignment.types'
import { formatColombo } from '@/lib/utils/datetime'

function getInitials(name: string) {
  return name.split(' ').slice(0, 2).map((n) => n[0]).join('').toUpperCase()
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

/** Build ordered hierarchy segments from the broadest to the most specific. */
function resolveGeoHierarchy(row: UserAssignmentDto): string[] {
  const parts: string[] = []
  if (row.regionName) parts.push(row.regionName)
  if (row.areaName) parts.push(row.areaName)
  if (row.territoryName) parts.push(row.territoryName)
  if (row.divisionName) parts.push(row.divisionName)
  return parts
}

export interface UserGeoAssignmentColumnActions {
  openEdit: (id: number) => void
  openDeactivate: (id: number) => void
  openActivate: (id: number) => void
}

export function getUserGeoAssignmentColumns(
  actions: UserGeoAssignmentColumnActions,
): ColumnDef<UserAssignmentDto>[] {
  const { openEdit, openDeactivate, openActivate } = actions

  return [
    {
      id: 'user',
      header: 'Name',
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
      id: 'role',
      header: 'Role',
      cell: ({ row }) => <RoleBadge role={row.original.userRole} />,
    },
    {
      id: 'geoLevel',
      header: 'Geo Level',
      cell: ({ row }) => {
        const r = row.original
        const level = r.divisionId ? 'Division' : r.territoryId ? 'Territory' : r.areaId ? 'Area' : r.regionId ? 'Region' : '—'
        return <span className="text-sm text-muted-foreground">{level}</span>
      },
    },
    {
      id: 'assignedTo',
      header: 'Assigned To',
      cell: ({ row }) => {
        const parts = resolveGeoHierarchy(row.original)
        if (parts.length === 0) return <span className="text-sm text-muted-foreground">—</span>
        const deepest = parts[parts.length - 1]
        const ancestors = parts.slice(0, -1)
        return (
          <div className="flex flex-col gap-0.5 min-w-0">
            <span className="text-sm font-semibold leading-tight truncate">{deepest}</span>
            {ancestors.length > 0 && (
              <span className="flex items-center gap-0.5 text-[11px] text-muted-foreground leading-tight">
                {ancestors.map((a, i) => (
                  <span key={i} className="flex items-center gap-0.5">
                    {i > 0 && <span className="opacity-40">›</span>}
                    <span className="truncate">{a}</span>
                  </span>
                ))}
              </span>
            )}
          </div>
        )
      },
    },
    {
      id: 'reportsTo',
      header: 'Reports To',
      cell: ({ row }) => {
        const name = row.original.reportsToUserName
        if (!name) return <span className="text-sm text-muted-foreground">—</span>
        return (
          <div className="flex items-center gap-2">
            <div className="flex h-7 w-7 shrink-0 items-center justify-center rounded-full bg-muted text-xs font-semibold text-muted-foreground">
              {getInitials(name)}
            </div>
            <span className="text-sm">{name}</span>
          </div>
        )
      },
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
      accessorKey: 'effectiveFrom',
      header: 'Assigned On',
      cell: ({ row }) => (
        <span className="text-sm text-muted-foreground">
          {formatColombo(row.original.effectiveFrom, 'd MMM yyyy')}
        </span>
      ),
    },
    {
      id: 'actions',
      size: 70,
      header: 'Actions',
      cell: ({ row }) => {
        const item = row.original
        return (
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button variant="ghost" size="icon" className="h-8 w-8">
                <MoreHorizontal className="h-4 w-4" />
                <span className="sr-only">Open menu</span>
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end">
              <DropdownMenuItem onClick={() => openEdit(item.id)}>
                Edit Assignment
              </DropdownMenuItem>
              <DropdownMenuSeparator />
              {item.isActive ? (
                <DropdownMenuItem
                  onClick={() => openDeactivate(item.id)}
                  className="text-destructive focus:text-destructive"
                >
                  Deactivate
                </DropdownMenuItem>
              ) : (
                <DropdownMenuItem onClick={() => openActivate(item.id)}>
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
