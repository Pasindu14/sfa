'use client'

import { useCallback } from 'react'
import { DataTable } from '@/components/data-table/data-table'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { useMyOutletDataTable } from '../../hooks/outlet.hooks'
import { getOutletPortalColumns } from '../columns/outlet-portal-columns'

export function OutletPortalPage() {
  const getColumns = useCallback(() => getOutletPortalColumns(), [])

  return (
    <div className="flex flex-col gap-6 p-6">
      <div className="flex items-center justify-between bg-muted/90 p-10 rounded-lg">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">My Outlets</h1>
          <p className="text-muted-foreground">Outlets in your territory</p>
        </div>
      </div>

      <DataTable
        config={{
          enableRowSelection: false,
          enableSearch: true,
          enableDateFilter: false,
          enableExport: false,
          enableColumnResizing: true,
          enableUrlState: false,
          columnResizingTableId: 'portal-outlets-table',
          searchPlaceholder: 'Search outlets...',
        }}
        getColumns={getColumns}
        fetchDataFn={useMyOutletDataTable}
        exportConfig={{
          entityName: 'outlets',
          columnMapping: {
            name: 'Name',
            tel: 'Telephone',
            address: 'Address',
            outletType: 'Type',
            outletCategory: 'Category',
            routeName: 'Route',
            territoryName: 'Territory',
            isActive: 'Status',
          },
          columnWidths: [
            { wch: 25 },
            { wch: 15 },
            { wch: 35 },
            { wch: 12 },
            { wch: 12 },
            { wch: 20 },
            { wch: 20 },
            { wch: 10 },
          ],
          headers: ['Name', 'Telephone', 'Address', 'Type', 'Category', 'Route', 'Territory', 'Status'],
        }}
        idField="id"
        renderCustomFilters={(filters, setFilters) => (
          <Select
            value={(filters?.status as string) ?? ''}
            onValueChange={(value) =>
              setFilters({ ...filters, status: value === 'all' ? '' : value })
            }
          >
            <SelectTrigger className="h-8 w-32">
              <SelectValue placeholder="All Statuses" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">All Statuses</SelectItem>
              <SelectItem value="active">Active</SelectItem>
              <SelectItem value="inactive">Inactive</SelectItem>
            </SelectContent>
          </Select>
        )}
      />
    </div>
  )
}
