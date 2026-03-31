'use client'

import { useMutation, useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import { ClipboardList } from 'lucide-react'
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from '@/components/ui/alert-dialog'
import { handleErrorToast } from '@/lib/hooks/use-error-toast'
import { createGrnAction } from '@/features/grn/actions/grn.actions'
import { grnKeys } from '@/features/grn/hooks/grn.hooks'
import { salesInvoiceKeys } from '../../hooks/sales-invoice.hooks'
import { useCreateGrnDialog } from '../../store'
import type { ActionFailure } from '@/lib/types/actions'

export function SalesInvoiceCreateGrnDialog() {
  const { isOpen, selectedId, close } = useCreateGrnDialog()
  const queryClient = useQueryClient()

  const { mutate, isPending } = useMutation({
    mutationFn: async (salesInvoiceId: number) => {
      const result = await createGrnAction({ salesInvoiceId })
      if (!result.success) throw result
      return result.data
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: salesInvoiceKeys.all })
      queryClient.invalidateQueries({ queryKey: grnKeys.all })
      close()
      toast.success('GRN created successfully')
    },
    onError: (error: ActionFailure) => {
      handleErrorToast(error, 'GRN', 'create')
    },
  })

  function handleConfirm() {
    if (selectedId) mutate(selectedId)
  }

  return (
    <AlertDialog open={isOpen} onOpenChange={(v) => { if (!v) close() }}>
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle className="flex items-center gap-2">
            <ClipboardList className="h-4 w-4 text-muted-foreground" />
            Create Goods Received Note
          </AlertDialogTitle>
          <AlertDialogDescription>
            A GRN will be created for this invoice and its status will change to{' '}
            <span className="font-medium text-foreground">GRN Received</span>.
            You can confirm the GRN once goods are physically received.
          </AlertDialogDescription>
        </AlertDialogHeader>
        <AlertDialogFooter>
          <AlertDialogCancel disabled={isPending}>Cancel</AlertDialogCancel>
          <AlertDialogAction onClick={handleConfirm} disabled={isPending}>
            {isPending ? 'Creating…' : 'Create GRN'}
          </AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  )
}
