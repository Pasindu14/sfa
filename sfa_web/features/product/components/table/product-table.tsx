'use client'

import { useCallback } from 'react'
import { DataTable } from '@/components/data-table/data-table'
import { Button } from '@/components/ui/button'
import { Plus } from 'lucide-react'
import {
  useEditDialog,
  useDeactivateDialog,
  useDeleteDialog,
  useActivateDialog,
  useProductDialogStore,
} from '../../store'
import { useProductDataTable } from '../../hooks/product.hooks'
import { getProductColumns } from '../columns/product-columns'

export function ProductTable() {
  const openCreate = useProductDialogStore((s) => s.openCreate)
  const { open: openEdit } = useEditDialog()
  const { open: openDeactivate } = useDeactivateDialog()
  const { open: openDelete } = useDeleteDialog()
  const { open: openActivate } = useActivateDialog()

  const getColumns = useCallback(
    () => getProductColumns({ openEdit, openDeactivate, openDelete, openActivate }),
    [openEdit, openDeactivate, openDelete, openActivate]
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
        columnResizingTableId: 'products-table',
        searchPlaceholder: 'Search by code or description...',
      }}
      getColumns={getColumns}
      fetchDataFn={useProductDataTable}
      exportConfig={{
        entityName: 'products',
        columnMapping: {
          code: 'Code',
          itemDescription: 'Item Description',
          printDescription: 'Print Description',
          piecesPerPack: 'Pcs / Pack',
          isActive: 'Status',
        },
        columnWidths: [{ wch: 15 }, { wch: 40 }, { wch: 30 }, { wch: 12 }, { wch: 12 }],
        headers: ['Code', 'Item Description', 'Print Description', 'Pcs / Pack', 'Status'],
      }}
      idField="id"
      renderToolbarContent={() => (
        <Button onClick={openCreate} className="gap-2">
          <Plus className="h-4 w-4" />
          Add Product
        </Button>
      )}
    />
  )
}
