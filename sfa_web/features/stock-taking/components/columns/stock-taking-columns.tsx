'use client'

import type { ColumnDef } from '@tanstack/react-table'
import { MoreHorizontal, Lock, LockOpen, Eye } from 'lucide-react'
import { useRouter } from 'next/navigation'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'
import type { StockTakingPeriodDto } from '../../schema/stock-taking.schema'
import { formatColombo } from '@/lib/utils/datetime'

const MONTHS = [
  '', 'January', 'February', 'March', 'April', 'May', 'June',
  'July', 'August', 'September', 'October', 'November', 'December',
]

export interface StockTakingColumnActions {
  openLock: (id: number) => void
  openUnlock: (id: number) => void
}

export function getStockTakingColumns(actions: StockTakingColumnActions): ColumnDef<StockTakingPeriodDto>[] {
  const { openLock, openUnlock } = actions

  return [
    {
      id: 'period',
      header: 'Period',
      cell: ({ row }) => {
        const { month, year } = row.original
        return (
          <div>
            <div className="text-sm font-medium">{MONTHS[month]} {year}</div>
            <div className="text-xs text-muted-foreground">Month {month}</div>
          </div>
        )
      },
    },
    {
      id: 'status',
      header: 'Status',
      cell: ({ row }) => {
        const { status } = row.original
        const isLocked = status === 'Locked'
        return (
          <Badge variant={isLocked ? 'secondary' : 'default'} className={isLocked ? 'text-amber-700 bg-amber-100' : 'bg-emerald-100 text-emerald-700'}>
            {isLocked ? <Lock className="h-3 w-3 mr-1" /> : <LockOpen className="h-3 w-3 mr-1" />}
            {status}
          </Badge>
        )
      },
    },
    {
      id: 'lockedBy',
      header: 'Locked By',
      cell: ({ row }) => {
        const { lockedByName, lockedAt } = row.original
        if (!lockedByName) return <span className="text-xs text-muted-foreground">—</span>
        return (
          <div>
            <div className="text-sm">{lockedByName}</div>
            {lockedAt && (
              <div className="text-xs text-muted-foreground">
                {formatColombo(lockedAt, 'd MMM yyyy')}
              </div>
            )}
          </div>
        )
      },
    },
    {
      id: 'actions',
      header: 'Actions',
      cell: function ActionsCell({ row }) {
        const item = row.original
        const router = useRouter()
        const isLocked = item.status === 'Locked'

        return (
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button variant="ghost" size="icon" className="h-8 w-8">
                <MoreHorizontal className="h-4 w-4" />
                <span className="sr-only">Open menu</span>
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end">
              <DropdownMenuItem onClick={() => router.push(`/stock-taking/${item.id}`)}>
                <Eye className="h-4 w-4 mr-2" />
                View Submissions
              </DropdownMenuItem>
              <DropdownMenuSeparator />
              {isLocked ? (
                <DropdownMenuItem onClick={() => openUnlock(item.id)}>
                  <LockOpen className="h-4 w-4 mr-2" />
                  Unlock Period
                </DropdownMenuItem>
              ) : (
                <DropdownMenuItem
                  onClick={() => openLock(item.id)}
                  className="text-amber-600 focus:text-amber-600"
                >
                  <Lock className="h-4 w-4 mr-2" />
                  Lock Period
                </DropdownMenuItem>
              )}
            </DropdownMenuContent>
          </DropdownMenu>
        )
      },
    },
  ]
}
