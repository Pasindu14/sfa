'use client'

import Link from 'next/link'
import type { ColumnDef } from '@tanstack/react-table'
import { MoreHorizontal, Star } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'
import { formatColombo } from '@/lib/utils/datetime'
import type { PricingStructureSelection } from '../../store'
import type { PricingStructureDto } from '../types/pricing-structure.types'

export interface PricingStructureColumnActions {
  openEdit: (id: number) => void
  openDuplicate: (selection: PricingStructureSelection) => void
  openSetDefault: (selection: PricingStructureSelection) => void
  openActivate: (selection: PricingStructureSelection) => void
  openDeactivate: (selection: PricingStructureSelection) => void
  openDelete: (selection: PricingStructureSelection) => void
}

export const pricesHref = (id: number) => `/pricing-structures/${id}`

export function DefaultBadge() {
  return (
    <Badge
      variant="outline"
      className="gap-1 border-amber-300 bg-amber-50 text-amber-800 dark:border-amber-700 dark:bg-amber-900/30 dark:text-amber-300"
    >
      <Star className="size-3 fill-current" />
      Default
    </Badge>
  )
}

export function StatusBadge({ isActive }: { isActive: boolean }) {
  return (
    <Badge variant={isActive ? 'default' : 'secondary'}>{isActive ? 'Active' : 'Inactive'}</Badge>
  )
}

export function getPricingStructureColumns(
  actions: PricingStructureColumnActions
): ColumnDef<PricingStructureDto>[] {
  const { openEdit, openDuplicate, openSetDefault, openActivate, openDeactivate, openDelete } =
    actions

  return [
    {
      accessorKey: 'name',
      header: 'Name',
      cell: ({ row }) => (
        <div className="flex items-center gap-2">
          <Link
            href={pricesHref(row.original.id)}
            className="text-sm font-medium hover:underline"
          >
            {row.original.name}
          </Link>
          {row.original.isDefault && <DefaultBadge />}
        </div>
      ),
    },
    {
      accessorKey: 'description',
      header: 'Description',
      cell: ({ row }) => (
        <span className="line-clamp-1 max-w-xs text-sm text-muted-foreground">
          {row.original.description || '—'}
        </span>
      ),
    },
    {
      accessorKey: 'pricedCount',
      header: 'Priced Products',
      cell: ({ row }) => (
        <span className="text-sm font-medium tabular-nums">{row.original.pricedCount}</span>
      ),
    },
    {
      accessorKey: 'isActive',
      header: 'Status',
      cell: ({ row }) => <StatusBadge isActive={row.original.isActive} />,
    },
    {
      accessorKey: 'updatedAt',
      header: 'Updated',
      cell: ({ row }) => (
        <span className="text-sm text-muted-foreground">
          {formatColombo(row.original.updatedAt, 'd MMM yyyy, HH:mm')}
        </span>
      ),
    },
    {
      id: 'actions',
      size: 70,
      header: 'Actions',
      cell: ({ row }) => {
        const item = row.original
        const selection: PricingStructureSelection = {
          id: item.id,
          name: item.name,
          rowVersion: item.rowVersion,
        }
        return (
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button variant="ghost" size="icon" className="h-8 w-8">
                <MoreHorizontal className="h-4 w-4" />
                <span className="sr-only">Open menu</span>
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end">
              <DropdownMenuItem asChild>
                <Link href={pricesHref(item.id)}>Edit prices</Link>
              </DropdownMenuItem>
              <DropdownMenuItem onClick={() => openEdit(item.id)}>Edit</DropdownMenuItem>
              <DropdownMenuItem onClick={() => openDuplicate(selection)}>
                Duplicate
              </DropdownMenuItem>
              {/* The API refuses an inactive default, so say why instead of letting it 422. */}
              {!item.isDefault && (
                <DropdownMenuItem
                  disabled={!item.isActive}
                  onClick={() => openSetDefault(selection)}
                  className="flex-col items-start gap-0"
                >
                  <span>Set as default</span>
                  {!item.isActive && (
                    <span className="text-[11px] text-muted-foreground">Activate it first</span>
                  )}
                </DropdownMenuItem>
              )}
              {/* The default can be neither deactivated nor deleted — reps would be left
                  with nothing to price from. Move the default first. */}
              {!item.isDefault && (
                <>
                  <DropdownMenuSeparator />
                  {item.isActive ? (
                    <DropdownMenuItem
                      onClick={() => openDeactivate(selection)}
                      className="text-destructive focus:text-destructive"
                    >
                      Deactivate
                    </DropdownMenuItem>
                  ) : (
                    <DropdownMenuItem onClick={() => openActivate(selection)}>
                      Activate
                    </DropdownMenuItem>
                  )}
                  <DropdownMenuSeparator />
                  <DropdownMenuItem
                    className="text-destructive focus:text-destructive"
                    onClick={() => openDelete(selection)}
                  >
                    Delete
                  </DropdownMenuItem>
                </>
              )}
            </DropdownMenuContent>
          </DropdownMenu>
        )
      },
    },
  ]
}
