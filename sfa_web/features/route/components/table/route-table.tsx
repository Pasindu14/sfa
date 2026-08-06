'use client'

import { useCallback, useState } from 'react'
import { DataTable } from '@/components/data-table/data-table'
import { Button } from '@/components/ui/button'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { Plus, RotateCcw, Loader2 } from 'lucide-react'
import {
  useEditDialog,
  useDeleteDialog,
  useActivateDialog,
  useDeactivateDialog,
  useRouteDialogStore,
} from '../../store'
import { useRouteDataTable } from '../../hooks/route.hooks'
import { getRouteColumns } from '../columns/route-columns'
import { useActiveAreas } from '@/features/area/hooks/area.hooks'
import { useActiveTerritories } from '@/features/territory/hooks/territory.hooks'

export function RouteTable() {
  const openCreate = useRouteDialogStore((s) => s.openCreate)
  const { open: openEdit } = useEditDialog()
  const { open: openDelete } = useDeleteDialog()
  const { open: openActivate } = useActivateDialog()
  const { open: openDeactivate } = useDeactivateDialog()
  const { data: areas = [], isLoading: loadingAreas } = useActiveAreas()

  // Territories are scoped to the selected area and only fetched once one is chosen —
  // mirrored from the DataTable's own customFilters state (see renderCustomFilters below)
  // since that state isn't otherwise exposed to the parent component.
  const [selectedAreaId, setSelectedAreaId] = useState('')
  const { data: territories = [], isLoading: loadingTerritories } = useActiveTerritories(
    selectedAreaId ? Number(selectedAreaId) : undefined,
    { enabled: !!selectedAreaId },
  )

  const getColumns = useCallback(
    () => getRouteColumns({ openEdit, openDelete, openActivate, openDeactivate }),
    [openEdit, openDelete, openActivate, openDeactivate],
  );

  return (
    <DataTable
      config={{
        enableRowSelection: false,
        enableSearch: true,
        enableDateFilter: false,
        enableExport: false,
        enableColumnResizing: true,
        enableUrlState: false,
        columnResizingTableId: 'routes-table',
        searchPlaceholder: 'Search routes...',
      }}
      getColumns={getColumns}
      fetchDataFn={useRouteDataTable}
      renderCustomFilters={(filters, setFilters) => {
        const hasActiveFilters = !!(filters?.areaId || filters?.territoryId)
        return (
          <div className="flex flex-wrap items-center gap-2">
            <Select
              value={(filters?.areaId as string) ?? 'all'}
              onValueChange={(value) => {
                const areaId = value === 'all' ? '' : value
                setSelectedAreaId(areaId)
                // Changing (or clearing) the area invalidates any previously selected
                // territory, since the territory list is scoped to the chosen area.
                setFilters({ ...filters, areaId, territoryId: '' })
              }}
            >
              <SelectTrigger className="h-8 w-40 sm:w-48">
                {loadingAreas ? (
                  <span className="flex items-center gap-1.5 text-muted-foreground">
                    <Loader2 className="h-3 w-3 animate-spin" />
                    Loading...
                  </span>
                ) : (
                  <SelectValue placeholder="Area" />
                )}
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">All Areas</SelectItem>
                {areas.map((a) => (
                  <SelectItem key={a.id} value={String(a.id)}>{a.name}</SelectItem>
                ))}
              </SelectContent>
            </Select>

            <Select
              disabled={!selectedAreaId}
              value={(filters?.territoryId as string) ?? 'all'}
              onValueChange={(value) =>
                setFilters({ ...filters, territoryId: value === 'all' ? '' : value })
              }
            >
              <SelectTrigger className="h-8 w-40 sm:w-48">
                {loadingTerritories ? (
                  <span className="flex items-center gap-1.5 text-muted-foreground">
                    <Loader2 className="h-3 w-3 animate-spin" />
                    Loading...
                  </span>
                ) : (
                  <SelectValue placeholder={selectedAreaId ? 'Territory' : 'Select an area first'} />
                )}
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">All Territories</SelectItem>
                {territories.map((t) => (
                  <SelectItem key={t.id} value={String(t.id)}>{t.name}</SelectItem>
                ))}
              </SelectContent>
            </Select>

            {hasActiveFilters && (
              <Button
                variant="ghost"
                size="sm"
                className="h-8 gap-1.5 text-muted-foreground"
                onClick={() => {
                  setSelectedAreaId('')
                  setFilters({ areaId: '', territoryId: '' })
                }}
              >
                <RotateCcw className="h-3.5 w-3.5" />
                Reset
              </Button>
            )}
          </div>
        )
      }}
      exportConfig={{
        entityName: 'routes',
        columnMapping: {
          name: 'Name',
          pinColor: 'Pin Color',
          divisionName: 'Division',
          territoryName: 'Territory',
          areaName: 'Area',
          regionName: 'Region',
          createdAt: 'Created At',
        },
        columnWidths: [
          { wch: 30 },
          { wch: 12 },
          { wch: 25 },
          { wch: 25 },
          { wch: 25 },
          { wch: 25 },
          { wch: 20 },
        ],
        headers: ['Name', 'Pin Color', 'Division', 'Territory', 'Area', 'Region', 'Created At'],
      }}
      idField="id"
      renderToolbarContent={() => (
        <Button onClick={openCreate} className="gap-2">
          <Plus className="h-4 w-4" />
          Add Route
        </Button>
      )}
    />
  )
}
