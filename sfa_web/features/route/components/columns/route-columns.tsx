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
import type { RouteDto } from '../types/route.types'
import { formatColombo } from '@/lib/utils/datetime'

export interface RouteColumnActions {
  openEdit: (id: number) => void
  openDelete: (id: number) => void
  openActivate: (id: number) => void
  openDeactivate: (id: number) => void
}

export function getRouteColumns(actions: RouteColumnActions): ColumnDef<RouteDto>[] {
  const { openEdit, openDelete, openActivate, openDeactivate } = actions

  return [
    {
      accessorKey: 'name',
      header: 'Name',
      cell: ({ row }) => (
        <div className="flex items-center gap-3">
          <div
            className="flex h-8 w-8 shrink-0 items-center justify-center rounded-full text-xs font-semibold text-white"
            style={{ backgroundColor: row.original.pinColor || '#3b82f6' }}
          >
            {row.original.name.substring(0, 2).toUpperCase()}
          </div>
          <span className="text-sm font-medium">{row.original.name}</span>
        </div>
      ),
    },
    {
      accessorKey: 'divisionName',
      header: 'Division',
      cell: ({ row }) => (
        <span className="text-sm text-muted-foreground">{row.original.divisionName}</span>
      ),
    },
    {
      accessorKey: 'territoryName',
      header: 'Territory',
      cell: ({ row }) => (
        <span className="text-sm text-muted-foreground">{row.original.territoryName}</span>
      ),
    },
    {
      accessorKey: 'areaName',
      header: 'Area',
      cell: ({ row }) => (
        <span className="text-sm text-muted-foreground">{row.original.areaName}</span>
      ),
    },
    {
      accessorKey: 'regionName',
      header: 'Region',
      cell: ({ row }) => (
        <span className="text-sm text-muted-foreground">{row.original.regionName}</span>
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
      accessorKey: 'createdAt',
      header: 'Created',
      cell: ({ row }) => (
        <span className="text-sm text-muted-foreground">
          {formatColombo(row.original.createdAt, 'd MMM yyyy')}
        </span>
      ),
    },
    {
      id: 'actions',
      header: 'Actions',
      cell: ({ row }) => {
        const route = row.original
        return (
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button variant="ghost" size="icon" className="h-8 w-8">
                <MoreHorizontal className="h-4 w-4" />
                <span className="sr-only">Open menu</span>
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end">
              <DropdownMenuItem onClick={() => openEdit(route.id)}>Edit</DropdownMenuItem>
              {route.isActive ? (
                <DropdownMenuItem onClick={() => openDeactivate(route.id)}>
                  Deactivate
                </DropdownMenuItem>
              ) : (
                <DropdownMenuItem onClick={() => openActivate(route.id)}>
                  Activate
                </DropdownMenuItem>
              )}
              <DropdownMenuSeparator />
              <DropdownMenuItem
                className="text-destructive"
                onClick={() => openDelete(route.id)}
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
