'use client'

import { useCallback, useState } from 'react'
import { Upload } from 'lucide-react'
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { Button } from '@/components/ui/button'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { AsyncSelect } from '@/components/async-select'
import { DataTable } from '@/components/data-table/data-table'
import { SalesTargetImportDialog } from '../dialogs/sales-target-import-dialog'
import { SalesTargetEditDialog } from '../dialogs/sales-target-edit-dialog'
import { useImportTargetDialog, useEditTargetDialog } from '../../store/sales-target-dialog.store'
import { useSalesTargetsDataTable, useImportBatchesDataTable } from '../../hooks/sales-target.hooks'
import { getTargetsColumns } from '../columns/targets-data-table-columns'
import { getImportHistoryColumns } from '../columns/import-history-data-table-columns'
import { searchSalesRepsAction } from '../../actions/sales-target.actions'
import type { SalesTargetDto } from '../../schema/sales-target.schema'
import type { UserDto } from '@/features/user/schema/user.schema'

const CURRENT_YEAR = new Date().getFullYear()
const YEARS = Array.from({ length: CURRENT_YEAR - 2019 }, (_, i) => CURRENT_YEAR - i)

const MONTHS = [
  { value: '1', label: 'January' }, { value: '2', label: 'February' },
  { value: '3', label: 'March' }, { value: '4', label: 'April' },
  { value: '5', label: 'May' }, { value: '6', label: 'June' },
  { value: '7', label: 'July' }, { value: '8', label: 'August' },
  { value: '9', label: 'September' }, { value: '10', label: 'October' },
  { value: '11', label: 'November' }, { value: '12', label: 'December' },
]

async function fetchSalesReps(search?: string): Promise<UserDto[]> {
  if (!search?.trim()) return []
  const result = await searchSalesRepsAction(search.trim())
  if (!result.success) throw new Error(result.error || 'Failed to search reps')
  return result.data
}

export function SalesTargetListPage() {
  const { open: openImport } = useImportTargetDialog()
  const { open: openEdit } = useEditTargetDialog()
  const [editTarget, setEditTarget] = useState<SalesTargetDto | null>(null)

  const handleEdit = useCallback((target: SalesTargetDto) => {
    setEditTarget(target)
    openEdit(target.id)
  }, [openEdit])

  const getTargetCols = useCallback(() => getTargetsColumns(handleEdit), [handleEdit])
  const getBatchCols = useCallback(() => getImportHistoryColumns(), [])

  return (
    <div className="flex flex-col gap-6 p-6">
      <div className="flex items-center bg-muted/90 p-10 rounded-lg">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Sales Targets</h1>
          <p className="text-muted-foreground">Manage monthly item-wise sales targets by rep</p>
        </div>
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
              enableColumnResizing: true,
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
            renderToolbarContent={() => (
              <Button variant="outline" size="sm" onClick={openImport} className="gap-1.5">
                <Upload className="h-3.5 w-3.5" />
                Import Excel
              </Button>
            )}
            renderCustomFilters={(filters, setFilters) => (
              <>
                <Select
                  value={filters?.year?.toString() ?? 'all'}
                  onValueChange={(v) =>
                    setFilters({ ...filters, year: v === 'all' ? undefined : Number(v) })
                  }
                >
                  <SelectTrigger className="h-8 w-28">
                    <SelectValue placeholder="All Years" />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="all">All Years</SelectItem>
                    {YEARS.map((y) => (
                      <SelectItem key={y} value={y.toString()}>{y}</SelectItem>
                    ))}
                  </SelectContent>
                </Select>

                <Select
                  value={filters?.month?.toString() ?? 'all'}
                  onValueChange={(v) =>
                    setFilters({ ...filters, month: v === 'all' ? undefined : Number(v) })
                  }
                >
                  <SelectTrigger className="h-8 w-36">
                    <SelectValue placeholder="All Months" />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="all">All Months</SelectItem>
                    {MONTHS.map((m) => (
                      <SelectItem key={m.value} value={m.value}>{m.label}</SelectItem>
                    ))}
                  </SelectContent>
                </Select>

                <AsyncSelect<UserDto>
                  label="Sales Rep"
                  placeholder="All Reps"
                  fetcher={fetchSalesReps}
                  value={filters?.salesRepId?.toString() ?? ''}
                  onChange={(v) =>
                    setFilters({ ...filters, salesRepId: v ? Number(v) : undefined })
                  }
                  getOptionValue={(u) => u.id.toString()}
                  getDisplayValue={(u) => <span className="text-sm">{u.name}</span>}
                  renderOption={(u) => (
                    <div className="flex flex-col gap-0.5 py-0.5">
                      <span className="text-sm font-medium">{u.name}</span>
                      <span className="text-xs text-muted-foreground font-mono">{u.id}</span>
                    </div>
                  )}
                  notFound={<p className="py-3 text-center text-sm text-muted-foreground">No reps found</p>}
                  triggerClassName="h-8 w-44"
                />
              </>
            )}
          />
        </TabsContent>

        <TabsContent value="history" className="mt-4">
          <DataTable
            config={{
              enableRowSelection: false,
              enableSearch: false,
              enableDateFilter: false,
              enableExport: false,
              enableColumnResizing: true,
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
      <SalesTargetEditDialog target={editTarget} />
    </div>
  )
}
