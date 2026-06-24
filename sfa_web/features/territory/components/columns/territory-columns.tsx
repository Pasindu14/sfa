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
import type { TerritoryDto } from '../types/territory.types'
import { formatColombo } from '@/lib/utils/datetime'

export interface TerritoryColumnActions {
  openEdit: (id: number) => void
  openDelete: (id: number) => void
  openActivate: (id: number) => void
  openDeactivate: (id: number) => void
}

export function getTerritoryColumns(actions: TerritoryColumnActions): ColumnDef<TerritoryDto>[] {
  const { openEdit, openDelete, openActivate, openDeactivate } = actions

  return [
    {
      accessorKey: 'name',
      header: 'Name',
      size: 400,
      cell: ({ row }) => (
        <div className="flex items-center gap-3">
          <div className="flex h-8 w-8 shrink-0 items-center justify-center rounded-full bg-muted text-xs font-semibold text-muted-foreground">
            {row.original.name.substring(0, 2).toUpperCase()}
          </div>
          <span className="text-sm font-medium">{row.original.name}</span>
        </div>
      ),
    },
    {
      accessorKey: 'areaName',
      header: 'Area',
      size: 180,
      cell: ({ row }) => (
        <span className="text-sm text-muted-foreground">{row.original.areaName}</span>
      ),
    },
    {
      accessorKey: 'isActive',
      header: 'Status',
      size: 110,
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
      size: 140,
      cell: ({ row }) => (
        <span className="text-sm text-muted-foreground">
          {formatColombo(row.original.createdAt, 'd MMM yyyy')}
        </span>
      ),
    },
    {
      id: 'actions',
      header: 'Actions',
      size: 90,
      cell: ({ row }) => {
        const territory = row.original
        return (
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button variant="ghost" size="icon" className="h-8 w-8">
                <MoreHorizontal className="h-4 w-4" />
                <span className="sr-only">Open menu</span>
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end">
              <DropdownMenuItem onClick={() => openEdit(territory.id)}>Edit</DropdownMenuItem>
              {territory.isActive ? (
                <DropdownMenuItem onClick={() => openDeactivate(territory.id)}>
                  Deactivate
                </DropdownMenuItem>
              ) : (
                <DropdownMenuItem onClick={() => openActivate(territory.id)}>
                  Activate
                </DropdownMenuItem>
              )}
              <DropdownMenuSeparator />
              <DropdownMenuItem
                className="text-destructive"
                onClick={() => openDelete(territory.id)}
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
