'use client'

import { useCallback } from 'react'
import { DataTable } from '@/components/data-table/data-table'
import { Button } from '@/components/ui/button'
import { Plus } from 'lucide-react'
import { useStockTakingDialogStore, useLockDialog, useUnlockDialog } from '../../store'
import { useStockTakingDataTable } from '../../hooks/stock-taking.hooks'
import { getStockTakingColumns } from '../columns/stock-taking-columns'

export function StockTakingTable() {
  const openCreate = useStockTakingDialogStore((s) => s.openCreate)
  const { open: openLock } = useLockDialog()
  const { open: openUnlock } = useUnlockDialog()

  const getColumns = useCallback(
    () => getStockTakingColumns({ openLock, openUnlock }),
    [openLock, openUnlock],
  )

  return (
    <DataTable
      config={{
        enableRowSelection: false,
        enableSearch: true,
        enableDateFilter: false,
        enableExport: false,
        enableColumnResizing: false,
        enableUrlState: false,
        searchPlaceholder: 'Search by year...',
      }}
      getColumns={getColumns}
      fetchDataFn={useStockTakingDataTable}
      exportConfig={{
        entityName: 'stock-taking-periods',
        columnMapping: { month: 'Month', year: 'Year', status: 'Status' },
        columnWidths: [{ wch: 15 }, { wch: 10 }, { wch: 12 }],
        headers: ['Month', 'Year', 'Status'],
      }}
      idField="id"
      renderToolbarContent={() => (
        <Button onClick={openCreate} className="gap-2">
          <Plus className="h-4 w-4" />
          Add Period
        </Button>
      )}
    />
  )
}
