'use client'

import { useCallback } from 'react'
import { DataTable } from '@/components/data-table/data-table'
import { Button } from '@/components/ui/button'
import { Plus } from 'lucide-react'
import {
  useEditDialog,
  useActivateDialog,
  useDeactivateDialog,
  useProductCategoryDialogStore,
} from '../../store'
import { useProductCategoryDataTable } from '../../hooks/product-category.hooks'
import { getProductCategoryColumns } from '../columns/product-category-columns'

export function ProductCategoryTable() {
  const openCreate = useProductCategoryDialogStore((s) => s.openCreate)
  const { open: openEdit } = useEditDialog()
  const { open: openActivate } = useActivateDialog()
  const { open: openDeactivate } = useDeactivateDialog()

  const getColumns = useCallback(
    () => getProductCategoryColumns({ openEdit, openActivate, openDeactivate }),
    [openEdit, openActivate, openDeactivate],
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
        columnResizingTableId: 'product-categories-table',
        searchPlaceholder: 'Search categories...',
      }}
      getColumns={getColumns}
      fetchDataFn={useProductCategoryDataTable}
      exportConfig={{
        entityName: 'product-categories',
        columnMapping: {
          name: 'Name',
          isActive: 'Status',
        },
        columnWidths: [{ wch: 30 }, { wch: 12 }],
        headers: ['Name', 'Status'],
      }}
      idField="id"
      renderToolbarContent={() => (
        <Button onClick={openCreate} className="gap-2">
          <Plus className="h-4 w-4" />
          Add Category
        </Button>
      )}
    />
  )
}
