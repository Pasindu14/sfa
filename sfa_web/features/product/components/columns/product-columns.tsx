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
import type { ProductDto } from '../types/product.types'

export interface ProductColumnActions {
  openEdit: (id: number) => void
  openDelete: (id: number) => void
}

export function getProductColumns(actions: ProductColumnActions): ColumnDef<ProductDto>[] {
  const { openEdit, openDelete } = actions

  return [
    {
      id: 'product',
      header: 'Product',
      cell: ({ row }) => {
        const { code, itemDescription } = row.original
        return (
          <div>
            <div className="text-sm font-medium">{code}</div>
            <div className="text-xs text-muted-foreground line-clamp-1 max-w-xs">
              {itemDescription}
            </div>
          </div>
        )
      },
    },
    {
      accessorKey: 'printDescription',
      header: 'Print Description',
      cell: ({ row }) => (
        <span className="text-sm text-muted-foreground">
          {row.original.printDescription ?? '—'}
        </span>
      ),
    },
    {
      accessorKey: 'piecesPerPack',
      header: 'Pcs / Pack',
      cell: ({ row }) => (
        <span className="text-sm font-medium tabular-nums">{row.original.piecesPerPack}</span>
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
              <DropdownMenuSeparator />
              <DropdownMenuItem
                onClick={() => openDelete(item.id)}
                className="text-destructive focus:text-destructive"
              >
                Deactivate
              </DropdownMenuItem>
            </DropdownMenuContent>
          </DropdownMenu>
        )
      },
    },
  ]
}
