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
import type { OutletDto } from '../types/outlet.types'

export interface OutletColumnActions {
  openEdit: (id: number) => void
  openDelete: (id: number) => void
  openActivate: (id: number) => void
  openDeactivate: (id: number) => void
}

export function getOutletColumns(actions: OutletColumnActions): ColumnDef<OutletDto>[] {
  const { openEdit, openDelete, openActivate, openDeactivate } = actions

  return [
    {
      id: 'name',
      header: 'Name',
      cell: ({ row }) => {
        const { name, id, outletType } = row.original
        return (
          <div className="flex items-center gap-3">
            <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-full bg-muted text-xs font-semibold text-muted-foreground">
              {name.substring(0, 2).toUpperCase()}
            </div>
            <div className="flex flex-col gap-0.5">
              <div className="text-sm font-medium">{name}</div>
              <div className="flex items-center gap-1.5 text-xs text-muted-foreground">
                <span className="w-fit rounded bg-muted px-1.5 py-0.5 text-[10px] font-medium">
                  #{id}
                </span>
                {outletType}
              </div>
            </div>
          </div>
        )
      },
    },
    {
      id: 'route',
      header: 'Route',
      cell: ({ row }) => {
        const { routeName, divisionName } = row.original
        return (
          <div>
            <div className="text-sm font-medium">{routeName}</div>
            <div className="text-xs text-muted-foreground">{divisionName}</div>
          </div>
        )
      },
    },
    {
      id: 'hierarchy',
      header: 'Territory / Area / Region',
      cell: ({ row }) => {
        const { territoryName, areaName, regionName } = row.original
        return (
          <div className="text-xs">
            <div className="font-medium text-sm">{territoryName}</div>
            <div className="text-muted-foreground">
              {areaName} • {regionName}
            </div>
          </div>
        )
      },
    },
    {
      id: 'category',
      header: 'Type / Category',
      cell: ({ row }) => {
        const { outletType, outletCategory } = row.original
        return (
          <div className="text-xs">
            <div className="font-medium text-sm">{outletType}</div>
            <div className="text-muted-foreground">{outletCategory}</div>
          </div>
        )
      },
    },
    {
      id: 'location',
      header: 'Province / District',
      cell: ({ row }) => {
        const { provinceCode, districtCode } = row.original
        if (!provinceCode && !districtCode) {
          return <span className="text-xs text-muted-foreground">—</span>
        }
        return (
          <div className="text-xs text-muted-foreground">
            {provinceCode != null && <div>Province: {provinceCode}</div>}
            {districtCode != null && <div>District: {districtCode}</div>}
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
              <DropdownMenuItem onClick={() => openEdit(item.id)}>Edit</DropdownMenuItem>
              <DropdownMenuItem onClick={() => openDelete(item.id)}>Delete</DropdownMenuItem>
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
