'use client'

import type { ColumnDef } from '@tanstack/react-table'
import { ShieldOff } from 'lucide-react'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import {
  Tooltip,
  TooltipContent,
  TooltipProvider,
  TooltipTrigger,
} from '@/components/ui/tooltip'
import { formatColombo } from '@/lib/utils/datetime'
import { useRevokeDialog } from '../../store'
import {
  exemptionReasonLabels,
  type ProximityExemptionDto,
} from '../../schema/proximity-exemption.schema'

/// Whole days left, counted on the inclusive business date the admin picked.
/// A grant whose last day is today reads "Today", not "0 days".
function daysRemaining(validUntilDate: string): number | null {
  const parts = validUntilDate.split('T')[0].split('-').map(Number)
  if (parts.length !== 3 || parts.some(Number.isNaN)) return null
  const [y, m, d] = parts
  const until = Date.UTC(y, m - 1, d)
  const now = new Date()
  const today = Date.UTC(now.getUTCFullYear(), now.getUTCMonth(), now.getUTCDate())
  return Math.round((until - today) / 86_400_000)
}

function ExpiryCell({ validUntilDate }: { validUntilDate: string }) {
  const left = daysRemaining(validUntilDate)
  const label = formatColombo(validUntilDate)

  // Colour by urgency so a long-running grant stands out from one about to
  // lapse — the whole point of the page is spotting the ones nobody revisited.
  const tone =
    left === null
      ? 'text-muted-foreground'
      : left <= 0
        ? 'text-amber-600'
        : left <= 2
          ? 'text-amber-600'
          : 'text-foreground'

  return (
    <div className="flex flex-col">
      <span className={`text-sm font-medium ${tone}`}>{label}</span>
      <span className="text-muted-foreground text-xs">
        {left === null
          ? '—'
          : left < 0
            ? 'Expired'
            : left === 0
              ? 'Last day'
              : left === 1
                ? '1 day left'
                : `${left} days left`}
      </span>
    </div>
  )
}

function NotesCell({ notes }: { notes: string | null }) {
  if (!notes) return <span className="text-muted-foreground text-sm">—</span>
  if (notes.length <= 60) return <span className="text-sm">{notes}</span>

  return (
    <TooltipProvider>
      <Tooltip>
        <TooltipTrigger asChild>
          <span className="text-sm">{notes.slice(0, 60)}…</span>
        </TooltipTrigger>
        <TooltipContent className="max-w-sm whitespace-pre-wrap">{notes}</TooltipContent>
      </Tooltip>
    </TooltipProvider>
  )
}

export function getProximityExemptionColumns(): ColumnDef<ProximityExemptionDto>[] {
  return [
    {
      accessorKey: 'name',
      header: 'Sales Rep',
      cell: ({ row }) => (
        <div className="flex flex-col">
          <span className="text-sm font-medium">{row.original.name || '—'}</span>
          <span className="text-muted-foreground text-xs">@{row.original.username}</span>
        </div>
      ),
    },
    {
      accessorKey: 'reason',
      header: 'Reason',
      cell: ({ row }) => (
        <Badge variant="secondary" className="text-xs font-medium">
          {exemptionReasonLabels[row.original.reason] ?? row.original.reason}
        </Badge>
      ),
    },
    {
      accessorKey: 'validUntilDate',
      header: 'Applies Through',
      cell: ({ row }) => <ExpiryCell validUntilDate={row.original.validUntilDate} />,
    },
    {
      accessorKey: 'validFrom',
      header: 'Granted',
      cell: ({ row }) => (
        <div className="flex flex-col">
          <span className="text-sm">{formatColombo(row.original.validFrom)}</span>
          <span className="text-muted-foreground text-xs">
            {row.original.grantedByUserName ?? '—'}
          </span>
        </div>
      ),
    },
    {
      accessorKey: 'notes',
      header: 'Notes',
      cell: ({ row }) => <NotesCell notes={row.original.notes} />,
    },
    {
      id: 'actions',
      header: '',
      enableHiding: false,
      cell: ({ row }) => <RevokeButton exemption={row.original} />,
    },
  ]
}

function RevokeButton({ exemption }: { exemption: ProximityExemptionDto }) {
  const { open } = useRevokeDialog()

  return (
    <Button
      variant="outline"
      size="sm"
      onClick={() =>
        open({
          id: exemption.id,
          rowVersion: exemption.rowVersion,
          userId: exemption.userId,
          name: exemption.name || exemption.username,
        })
      }
    >
      <ShieldOff className="mr-2 h-3.5 w-3.5" />
      Revoke
    </Button>
  )
}
