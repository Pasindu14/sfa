'use client'

import type { ColumnDef } from '@tanstack/react-table'
import { MoreHorizontal, Package } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'
import type { PricingStructureDto } from '../types/pricing-structure.types'

export interface PricingStructureColumnActions {
  openEdit: (id: number) => void
  openDelete: (id: number) => void
  openActivate: (id: number) => void
  openManageItems: (id: number) => void
}

export function getPricingStructureColumns(
  actions: PricingStructureColumnActions
): ColumnDef<PricingStructureDto>[] {
  const { openEdit, openDelete, openActivate, openManageItems } = actions

  return [
    {
      id: 'name',
      header: 'Name',
      cell: ({ row }) => {
        const { name, isDefault } = row.original
        return (
          <div className="flex items-center gap-2">
            <div className="flex h-8 w-8 shrink-0 items-center justify-center rounded-full bg-muted text-xs font-semibold text-muted-foreground">
              {name.substring(0, 2).toUpperCase()}
            </div>
            <div>
              <div className="flex items-center gap-2">
                <span className="text-sm font-medium">{name}</span>
                {isDefault && (
                  <Badge variant="default" className="text-xs px-1.5 py-0">
                    Default
                  </Badge>
                )}
              </div>
            </div>
          </div>
        )
      },
    },
    {
      accessorKey: 'description',
      header: 'Description',
      cell: ({ row }) => (
        <span className="text-sm text-muted-foreground">
          {row.original.description || '—'}
        </span>
      ),
    },
    {
      accessorKey: 'itemCount',
      header: 'Products',
      cell: ({ row }) => (
        <div className="flex items-center gap-1 text-sm text-muted-foreground">
          <Package className="h-3.5 w-3.5" />
          <span>{row.original.itemCount}</span>
        </div>
      ),
    },
    {
      accessorKey: 'isActive',
      header: 'Status',
      cell: ({ row }) => (
        <Badge variant={row.original.isActive ? 'default' : 'secondary'}>
          {row.original.isActive ? 'Active' : 'Inactive'}
        </Badge>
      ),
    },
    {
      id: 'actions',
      header: '',
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
              <DropdownMenuItem onClick={() => openManageItems(item.id)}>
                Manage Items
              </DropdownMenuItem>
              <DropdownMenuSeparator />
              <DropdownMenuItem onClick={() => openEdit(item.id)}>Edit</DropdownMenuItem>
              {item.isActive ? (
                <DropdownMenuItem
                  onClick={() => openDelete(item.id)}
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
