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
  useOutletDialogStore,
} from '../../store'
import { useOutletDataTable } from '../../hooks/outlet.hooks'
import { getOutletColumns } from '../columns/outlet-columns'
import { useActiveTerritories } from '@/features/territory/hooks/territory.hooks'
import { useActiveRoutes } from '@/features/route/hooks/route.hooks'

export function OutletTable() {
  const openCreate = useOutletDialogStore((s) => s.openCreate)
  const { open: openEdit } = useEditDialog()
  const { open: openDelete } = useDeleteDialog()
  const { open: openActivate } = useActivateDialog()
  const { open: openDeactivate } = useDeactivateDialog()
  const { data: territories = [], isLoading: loadingTerritories } = useActiveTerritories()

  // Routes are scoped to the selected territory and only fetched once one is chosen —
  // mirrored from the DataTable's own customFilters state (see renderCustomFilters below)
  // since that state isn't otherwise exposed to the parent component.
  const [selectedTerritoryId, setSelectedTerritoryId] = useState('')
  const { data: routes = [], isLoading: loadingRoutes } = useActiveRoutes(
    selectedTerritoryId ? Number(selectedTerritoryId) : undefined,
    { enabled: !!selectedTerritoryId },
  )

  const getColumns = useCallback(
    () => getOutletColumns({ openEdit, openDelete, openActivate, openDeactivate }),
    [openEdit, openDelete, openActivate, openDeactivate],
  )

  return (
    <DataTable
      config={{
        enableRowSelection: false,
        enableSearch: true,
        enableDateFilter: false,
        enableExport: false,
        enableColumnResizing: true,
        enableUrlState: false,
        columnResizingTableId: 'outlets-table',
        searchPlaceholder: 'Search outlets...',
      }}
      getColumns={getColumns}
      fetchDataFn={useOutletDataTable}
      renderCustomFilters={(filters, setFilters) => {
        const hasActiveFilters = !!(filters?.territoryId || filters?.routeId)
        return (
          <div className="flex flex-wrap items-center gap-2">
            <Select
              value={(filters?.territoryId as string) ?? 'all'}
              onValueChange={(value) => {
                const territoryId = value === 'all' ? '' : value
                setSelectedTerritoryId(territoryId)
                // Changing (or clearing) the territory invalidates any previously selected
                // route, since the route list is scoped to the chosen territory.
                setFilters({ ...filters, territoryId, routeId: '' })
              }}
            >
              <SelectTrigger className="h-8 w-40 sm:w-48">
                {loadingTerritories ? (
                  <span className="flex items-center gap-1.5 text-muted-foreground">
                    <Loader2 className="h-3 w-3 animate-spin" />
                    Loading...
                  </span>
                ) : (
                  <SelectValue placeholder="Territory" />
                )}
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">All Territories</SelectItem>
                {territories.map((t) => (
                  <SelectItem key={t.id} value={String(t.id)}>{t.name}</SelectItem>
                ))}
              </SelectContent>
            </Select>

            <Select
              disabled={!selectedTerritoryId}
              value={(filters?.routeId as string) ?? 'all'}
              onValueChange={(value) =>
                setFilters({ ...filters, routeId: value === 'all' ? '' : value })
              }
            >
              <SelectTrigger className="h-8 w-40 sm:w-48">
                {loadingRoutes ? (
                  <span className="flex items-center gap-1.5 text-muted-foreground">
                    <Loader2 className="h-3 w-3 animate-spin" />
                    Loading...
                  </span>
                ) : (
                  <SelectValue placeholder={selectedTerritoryId ? 'Route' : 'Select a territory first'} />
                )}
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">All Routes</SelectItem>
                {routes.map((r) => (
                  <SelectItem key={r.id} value={String(r.id)}>{r.name}</SelectItem>
                ))}
              </SelectContent>
            </Select>

            {hasActiveFilters && (
              <Button
                variant="ghost"
                size="sm"
                className="h-8 gap-1.5 text-muted-foreground"
                onClick={() => {
                  setSelectedTerritoryId('')
                  setFilters({ territoryId: '', routeId: '' })
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
        entityName: 'outlets',
        columnMapping: {
          name: 'Name',
          nicNo: 'NIC No',
          tel: 'Telephone',
          email: 'Email',
          address: 'Address',
          outletType: 'Type',
          outletCategory: 'Category',
          routeName: 'Route',
          isActive: 'Status',
        },
        columnWidths: [
          { wch: 25 },
          { wch: 20 },
          { wch: 15 },
          { wch: 25 },
          { wch: 35 },
          { wch: 12 },
          { wch: 12 },
          { wch: 20 },
          { wch: 10 },
        ],
        headers: [
          'Name',
          'NIC No',
          'Telephone',
          'Email',
          'Address',
          'Type',
          'Category',
          'Route',
          'Status',
        ],
      }}
      idField="id"
      renderToolbarContent={() => (
        <Button onClick={openCreate} className="gap-2">
          <Plus className="h-4 w-4" />
          Add Outlet
        </Button>
      )}
    />
  )
}
