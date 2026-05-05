'use client'

import { useCallback } from 'react'
import { Upload } from 'lucide-react'
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { Button } from '@/components/ui/button'
import { DataTable } from '@/components/data-table/data-table'
import { SalesTargetImportDialog } from '../dialogs/sales-target-import-dialog'
import { useImportTargetDialog } from '../../store/sales-target-dialog.store'
import { useSalesTargetsDataTable, useImportBatchesDataTable } from '../../hooks/sales-target.hooks'
import { getTargetsColumns } from '../columns/targets-data-table-columns'
import { getImportHistoryColumns } from '../columns/import-history-data-table-columns'

export function SalesTargetListPage() {
  const { open: openImport } = useImportTargetDialog()
  const getTargetCols = useCallback(() => getTargetsColumns(), [])
  const getBatchCols = useCallback(() => getImportHistoryColumns(), [])

  return (
    <div className="flex flex-col gap-6 p-6">
      <div className="flex items-center justify-between bg-muted/90 p-10 rounded-lg">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Sales Targets</h1>
          <p className="text-muted-foreground">Manage monthly item-wise sales targets by rep</p>
        </div>
        <Button onClick={openImport} className="gap-2">
          <Upload className="h-4 w-4" />
          Import Excel
        </Button>
      </div>

      <Tabs defaultValue="targets">
        <TabsList>
          <TabsTrigger value="targets">Targets</TabsTrigger>
          <TabsTrigger value="history">Import History</TabsTrigger>
        </TabsList>

        <TabsContent value="targets" className="mt-4">
          <DataTable
            config={{
              enableRowSelection: false,
              enableSearch: true,
              enableDateFilter: false,
              enableExport: false,
              enableColumnResizing: false,
              enableUrlState: true,
              columnResizingTableId: 'sales-targets-table',
              searchPlaceholder: 'Search by rep name, product code…',
            }}
            getColumns={getTargetCols}
            fetchDataFn={useSalesTargetsDataTable}
            exportConfig={{
              entityName: 'sales-targets',
              columnMapping: {},
              columnWidths: [],
              headers: [],
            }}
            idField="id"
          />
        </TabsContent>

        <TabsContent value="history" className="mt-4">
          <DataTable
            config={{
              enableRowSelection: false,
              enableSearch: false,
              enableDateFilter: false,
              enableExport: false,
              enableColumnResizing: false,
              enableUrlState: false,
              columnResizingTableId: 'sales-target-batches-table',
              searchPlaceholder: '',
            }}
            getColumns={getBatchCols}
            fetchDataFn={useImportBatchesDataTable}
            exportConfig={{
              entityName: 'import-batches',
              columnMapping: {},
              columnWidths: [],
              headers: [],
            }}
            idField="id"
          />
        </TabsContent>
      </Tabs>

      <SalesTargetImportDialog />
    </div>
  )
}
