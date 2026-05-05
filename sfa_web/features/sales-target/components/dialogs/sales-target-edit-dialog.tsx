'use client'

import { useEffect } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import {
  Dialog, DialogContent, DialogHeader, DialogTitle, DialogDescription, DialogFooter,
} from '@/components/ui/dialog'
import {
  Form, FormControl, FormField, FormItem, FormLabel, FormMessage,
} from '@/components/ui/form'
import { Input } from '@/components/ui/input'
import { Button } from '@/components/ui/button'
import { Spinner } from '@/components/ui/spinner'
import { updateTargetQuantitySchema, type UpdateTargetQuantityInput } from '../../schema/sales-target.schema'
import { useEditTargetDialog } from '../../store/sales-target-dialog.store'
import { useUpdateSalesTarget } from '../../hooks/sales-target.hooks'
import type { SalesTargetDto } from '../../schema/sales-target.schema'

const MONTH_LABELS = [
  '', 'January', 'February', 'March', 'April', 'May', 'June',
  'July', 'August', 'September', 'October', 'November', 'December',
]

interface Props {
  target: SalesTargetDto | null
}

export function SalesTargetEditDialog({ target }: Props) {
  const { isOpen, close } = useEditTargetDialog()
  const { mutate, isPending } = useUpdateSalesTarget()

  const form = useForm<UpdateTargetQuantityInput>({
    resolver: zodResolver(updateTargetQuantitySchema),
    defaultValues: { targetQuantity: 0 },
  })

  useEffect(() => {
    if (target) {
      form.reset({ targetQuantity: target.targetQuantity })
    }
  }, [target, form])

  function handleSubmit(data: UpdateTargetQuantityInput) {
    if (!target) return
    mutate({ id: target.id, data })
  }

  function handleOpenChange(open: boolean) {
    if (!open && !isPending) {
      close()
      form.reset()
    }
  }

  return (
    <Dialog open={isOpen} onOpenChange={handleOpenChange}>
      <DialogContent className="sm:max-w-sm">
        <DialogHeader>
          <DialogTitle className="text-base">Edit Target Quantity</DialogTitle>
          <DialogDescription className="text-xs">
            Update the target quantity for this rep/product combination.
          </DialogDescription>
        </DialogHeader>

        {target && (
          <div className="rounded-lg border bg-muted/30 px-3 py-2.5 space-y-1 text-sm">
            <div className="flex justify-between gap-4">
              <span className="text-muted-foreground text-xs">Rep</span>
              <span className="font-medium text-xs text-right">
                <span className="font-mono">{target.salesRepId}</span>
                {' · '}
                {target.salesRepName}
              </span>
            </div>
            <div className="flex justify-between gap-4">
              <span className="text-muted-foreground text-xs">Product</span>
              <span className="font-medium text-xs text-right">
                <span className="font-mono">{target.productCode}</span>
                {' · '}
                <span className="text-muted-foreground">{target.productName}</span>
              </span>
            </div>
            <div className="flex justify-between gap-4">
              <span className="text-muted-foreground text-xs">Period</span>
              <span className="font-medium text-xs">{MONTH_LABELS[target.month]} {target.year}</span>
            </div>
            {target.supervisorName && (
              <div className="flex justify-between gap-4">
                <span className="text-muted-foreground text-xs">Supervisor</span>
                <span className="text-xs">{target.supervisorName}</span>
              </div>
            )}
          </div>
        )}

        <Form {...form}>
          <form onSubmit={form.handleSubmit(handleSubmit)} className="space-y-4">
            <FormField
              control={form.control}
              name="targetQuantity"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Target Quantity</FormLabel>
                  <FormControl>
                    <Input
                      type="number"
                      min={0}
                      step="any"
                      placeholder="0"
                      {...field}
                      onChange={(e) => field.onChange(parseFloat(e.target.value) || 0)}
                    />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />

            <DialogFooter>
              <Button type="button" variant="outline" size="sm" onClick={() => handleOpenChange(false)} disabled={isPending}>
                Cancel
              </Button>
              <Button type="submit" size="sm" disabled={isPending}>
                {isPending ? <><Spinner className="mr-1.5 h-3 w-3" />Saving…</> : 'Save'}
              </Button>
            </DialogFooter>
          </form>
        </Form>
      </DialogContent>
    </Dialog>
  )
}
