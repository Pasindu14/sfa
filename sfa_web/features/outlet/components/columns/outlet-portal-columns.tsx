'use client'

import Link from 'next/link'
import type { ColumnDef } from '@tanstack/react-table'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Receipt } from 'lucide-react'
import type { OutletDto } from '../types/outlet.types'

export function getOutletPortalColumns(): ColumnDef<OutletDto>[] {
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
            <div>
              <div className="text-sm font-medium">{name}</div>
              <div className="text-xs text-muted-foreground">
                ID: {id} • {outletType}
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
        const { tel, address } = row.original
        return (
          <div>
            <div className="text-sm font-medium">{tel}</div>
            <div className="text-xs text-muted-foreground truncate max-w-[200px]">{address}</div>
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
      id: 'territory',
      header: 'Territory',
      cell: ({ row }) => {
        const { territoryName, areaName } = row.original
        return (
          <div className="text-xs">
            <div className="font-medium text-sm">{territoryName}</div>
            <div className="text-muted-foreground">{areaName}</div>
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
      id: 'bills',
      header: '',
      cell: ({ row }) => {
        const { id, name } = row.original
        return (
          <Link href={`/portal/outlets/${id}/bills?name=${encodeURIComponent(name)}`}>
            <Button variant="outline" size="sm" className="gap-1.5 text-xs h-8">
              <Receipt className="h-3.5 w-3.5" />
              Bills
            </Button>
          </Link>
        )
      },
    },
  ]
}
