'use client'

import { useCallback } from 'react'
import { Plus } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { DataTable } from '@/components/data-table/data-table'
import { getProximityExemptionColumns } from '../columns/proximity-exemption-columns'
import { useProximityExemptionDataTable } from '../../hooks/proximity-exemption.hooks'
import { useGrantDialog } from '../../store'

export function ProximityExemptionTable() {
  const { open: openGrant } = useGrantDialog()
  const getColumns = useCallback(() => getProximityExemptionColumns(), [])

  return (
    <DataTable
      config={{
        enableRowSelection: false,
        enableSearch: true,
        enableDateFilter: false,
        enableExport: true,
        enableColumnResizing: true,
        enableUrlState: false,
        searchPlaceholder: 'Search by rep name or username...',
      }}
      getColumns={getColumns}
      fetchDataFn={useProximityExemptionDataTable}
      idField="id"
      exportConfig={{
        entityName: 'proximity-exemptions',
        columnMapping: {
          name: 'Sales Rep',
          username: 'Username',
          reason: 'Reason',
          validUntilDate: 'Applies Through',
          validFrom: 'Granted',
          grantedByUserName: 'Granted By',
          notes: 'Notes',
        },
        columnWidths: [
          { wch: 24 },
          { wch: 16 },
          { wch: 26 },
          { wch: 16 },
          { wch: 16 },
          { wch: 20 },
          { wch: 40 },
        ],
        headers: [
          'name',
          'username',
          'reason',
          'validUntilDate',
          'validFrom',
          'grantedByUserName',
          'notes',
        ],
      }}
      renderToolbarContent={() => (
        <Button onClick={openGrant} className="gap-2">
          <Plus className="h-4 w-4" />
          Grant Exemption
        </Button>
      )}
    />
  )
}
