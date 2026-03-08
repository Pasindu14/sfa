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
import type { DistributorDto } from '../types/distributor.types'

export interface DistributorColumnActions {
  openEdit: (id: number) => void
  openActivate: (id: number) => void
  openDeactivate: (id: number) => void
}

export function getDistributorColumns(actions: DistributorColumnActions): ColumnDef<DistributorDto>[] {
  const { openEdit, openActivate, openDeactivate } = actions

  return [
    {
      id: 'nameAlias',
      header: 'Distributor',
      cell: ({ row }) => {
        const { name, alias, id } = row.original
        return (
          <div className="flex items-center gap-3">
            <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-full bg-muted text-xs font-semibold text-muted-foreground">
              {name.substring(0, 2).toUpperCase()}
            </div>
            <div>
              <div className="text-sm font-medium">{name}</div>
              <div className="text-xs text-muted-foreground">
                ID: {id} • Alias: {alias}
              </div>
            </div>
          </div>
        )
      },
    },
    {
      id: 'contact',
      header: 'Contact',
      cell: ({ row }) => {
        const { email, phone } = row.original
        return (
          <div>
            <div className="text-sm">{email}</div>
            <div className="text-xs text-muted-foreground">{phone}</div>
          </div>
        )
      },
    },
    {
      accessorKey: 'address',
      header: 'Address',
      cell: ({ row }) => (
        <span className="text-sm text-muted-foreground line-clamp-2 max-w-xs">
          {row.original.address}
        </span>
      ),
    },
    {
      id: 'financials',
      header: 'Discount / Commission',
      cell: ({ row }) => {
        const { tradeDiscount, commission } = row.original
        return (
          <div className="text-sm">
            <span className="font-medium">{tradeDiscount}%</span>
            <span className="text-muted-foreground"> / </span>
            <span className="font-medium">{commission}%</span>
          </div>
        )
      },
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
              <DropdownMenuItem onClick={() => openEdit(item.id)}>Edit</DropdownMenuItem>
              {item.isActive ? (
                <DropdownMenuItem onClick={() => openDeactivate(item.id)}>
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
