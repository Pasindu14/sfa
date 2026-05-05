'use client'

import type { ColumnDef } from '@tanstack/react-table'
import { Badge } from '@/components/ui/badge'
import type { SalesTargetImportBatchDto } from '../../schema/sales-target.schema'

const MONTH_LABELS = [
  '', 'Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun',
  'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec',
]

function StatusBadge({ status }: { status: string }) {
  const variant =
    status === 'Completed' ? 'default' :
    status === 'PartialFailed' ? 'secondary' :
    'destructive'
  return <Badge variant={variant}>{status}</Badge>
}

export function getImportHistoryColumns(): ColumnDef<SalesTargetImportBatchDto>[] {
  return [
    {
      id: 'batch',
      header: 'Batch',
      cell: ({ row }) => (
        <div>
          <div className="font-mono text-xs font-semibold">{row.original.batchNumber}</div>
          <div className="text-xs text-muted-foreground truncate max-w-48">{row.original.fileName}</div>
        </div>
      ),
    },
    {
      id: 'period',
      header: 'Period',
      cell: ({ row }) => (
        <span className="text-sm tabular-nums">
          {MONTH_LABELS[row.original.month]} {row.original.year}
        </span>
      ),
    },
    {
      id: 'status',
      header: 'Status',
      cell: ({ row }) => <StatusBadge status={row.original.status} />,
    },
    {
      id: 'counts',
      header: 'Inserted / Updated / Skipped',
      cell: ({ row }) => {
        const { insertedRows, updatedRows, skippedRows, totalRows } = row.original
        return (
          <div className="flex items-center gap-1.5 text-xs tabular-nums">
            <span className="text-green-600 font-medium">{insertedRows}</span>
            <span className="text-muted-foreground">/</span>
            <span className="text-blue-600 font-medium">{updatedRows}</span>
            <span className="text-muted-foreground">/</span>
            <span className={skippedRows > 0 ? 'text-amber-600 font-medium' : 'text-muted-foreground'}>{skippedRows}</span>
            <span className="text-muted-foreground">({totalRows} total)</span>
          </div>
        )
      },
    },
    {
      accessorKey: 'importedByName',
      header: 'Imported By',
      cell: ({ row }) => (
        <span className="text-sm text-muted-foreground">{row.original.importedByName}</span>
      ),
    },
    {
      id: 'importedAt',
      header: 'Date',
      cell: ({ row }) => (
        <span className="text-xs text-muted-foreground">
          {new Date(row.original.importedAt).toLocaleDateString('en-LK')}
        </span>
      ),
    },
  ]
}
