'use client'

import { useEffect } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
} from '@/components/ui/dialog'
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
import {
  Form,
  FormControl,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from '@/components/ui/form'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { Input } from '@/components/ui/input'
import { Button } from '@/components/ui/button'
import { Spinner } from '@/components/ui/spinner'
import { useCreateDialog, useLockDialog, useUnlockDialog, useAdjustDialog } from '../../store'
import { useCreatePeriod, useLockPeriod, useUnlockPeriod, useAdjustLine } from '../../hooks/stock-taking.hooks'
import { createPeriodSchema, adjustLineSchema, type CreatePeriodInput, type AdjustLineInput } from '../../schema/stock-taking.schema'

const MONTHS = [
  { value: '1', label: 'January' }, { value: '2', label: 'February' },
  { value: '3', label: 'March' }, { value: '4', label: 'April' },
  { value: '5', label: 'May' }, { value: '6', label: 'June' },
  { value: '7', label: 'July' }, { value: '8', label: 'August' },
  { value: '9', label: 'September' }, { value: '10', label: 'October' },
  { value: '11', label: 'November' }, { value: '12', label: 'December' },
]

const currentYear = new Date().getFullYear()
const YEARS = Array.from({ length: 5 }, (_, i) => currentYear - 1 + i)

// --- Create Period Dialog ---

function CreatePeriodDialog() {
  const { isOpen, close } = useCreateDialog()
  const { mutate, isPending, fieldErrors, clearFieldErrors } = useCreatePeriod()

  const form = useForm<CreatePeriodInput>({
    resolver: zodResolver(createPeriodSchema),
    defaultValues: { month: new Date().getMonth() + 1, year: currentYear },
  })

  useEffect(() => {
    if (fieldErrors) {
      Object.entries(fieldErrors).forEach(([field, message]) => {
        form.setError(field as keyof CreatePeriodInput, { message })
      })
    }
  }, [fieldErrors, form])

  return (
    <Dialog open={isOpen} onOpenChange={(open) => { if (!open) { close(); clearFieldErrors() } }}>
      <DialogContent className="max-w-sm">
        <DialogHeader>
          <DialogTitle>Create Stock Taking Period</DialogTitle>
          <DialogDescription>Open a new month for distributor stock counts.</DialogDescription>
        </DialogHeader>
        <Form {...form}>
          <form onSubmit={form.handleSubmit((data) => mutate(data))} className="space-y-4">
            <FormField
              control={form.control}
              name="month"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Month</FormLabel>
                  <Select
                    value={String(field.value)}
                    onValueChange={(v) => field.onChange(Number(v))}
                  >
                    <FormControl>
                      <SelectTrigger className="w-full">
                        <SelectValue placeholder="Select month" />
                      </SelectTrigger>
                    </FormControl>
                    <SelectContent>
                      {MONTHS.map((m) => (
                        <SelectItem key={m.value} value={m.value}>{m.label}</SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                  <FormMessage />
                </FormItem>
              )}
            />
            <FormField
              control={form.control}
              name="year"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Year</FormLabel>
                  <Select
                    value={String(field.value)}
                    onValueChange={(v) => field.onChange(Number(v))}
                  >
                    <FormControl>
                      <SelectTrigger className="w-full">
                        <SelectValue placeholder="Select year" />
                      </SelectTrigger>
                    </FormControl>
                    <SelectContent>
                      {YEARS.map((y) => (
                        <SelectItem key={y} value={String(y)}>{y}</SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                  <FormMessage />
                </FormItem>
              )}
            />
            <Button type="submit" className="w-full" disabled={isPending}>
              {isPending ? <Spinner className="mr-2" /> : null}
              Create Period
            </Button>
          </form>
        </Form>
      </DialogContent>
    </Dialog>
  )
}

// --- Lock Period Dialog ---

function LockPeriodDialog() {
  const { isOpen, selectedId, close } = useLockDialog()
  const { mutate, isPending } = useLockPeriod()

  return (
    <AlertDialog open={isOpen} onOpenChange={(open) => !open && close()}>
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>Lock Period</AlertDialogTitle>
          <AlertDialogDescription>
            Locking this period will prevent distributors from submitting or editing stock counts.
            You can unlock it again at any time.
          </AlertDialogDescription>
        </AlertDialogHeader>
        <AlertDialogFooter>
          <AlertDialogCancel disabled={isPending}>Cancel</AlertDialogCancel>
          <AlertDialogAction
            disabled={isPending}
            onClick={() => selectedId && mutate(selectedId)}
            className="bg-amber-600 text-white hover:bg-amber-700"
          >
            {isPending ? <Spinner className="mr-2" /> : null}
            Lock Period
          </AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  )
}

// --- Unlock Period Dialog ---

function UnlockPeriodDialog() {
  const { isOpen, selectedId, close } = useUnlockDialog()
  const { mutate, isPending } = useUnlockPeriod()

  return (
    <AlertDialog open={isOpen} onOpenChange={(open) => !open && close()}>
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>Unlock Period</AlertDialogTitle>
          <AlertDialogDescription>
            Unlocking will allow distributors to submit or edit their stock counts again.
          </AlertDialogDescription>
        </AlertDialogHeader>
        <AlertDialogFooter>
          <AlertDialogCancel disabled={isPending}>Cancel</AlertDialogCancel>
          <AlertDialogAction
            disabled={isPending}
            onClick={() => selectedId && mutate(selectedId)}
          >
            {isPending ? <Spinner className="mr-2" /> : null}
            Unlock Period
          </AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  )
}

// --- Adjust Line Dialog ---

export function AdjustLineDialog() {
  const { isOpen, selectedLineId, selectedLineCountedQty, close } = useAdjustDialog()
  const { mutate, isPending, fieldErrors, clearFieldErrors } = useAdjustLine()

  const form = useForm<AdjustLineInput>({
    resolver: zodResolver(adjustLineSchema),
    defaultValues: { adjustedQuantity: 0 },
  })

  useEffect(() => {
    if (isOpen) {
      form.setValue('adjustedQuantity', selectedLineCountedQty)
    }
  }, [isOpen, selectedLineCountedQty, form])

  useEffect(() => {
    if (fieldErrors) {
      Object.entries(fieldErrors).forEach(([field, message]) => {
        form.setError(field as keyof AdjustLineInput, { message })
      })
    }
  }, [fieldErrors, form])

  return (
    <Dialog open={isOpen} onOpenChange={(open) => { if (!open) { close(); clearFieldErrors() } }}>
      <DialogContent className="max-w-sm">
        <DialogHeader>
          <DialogTitle>Adjust System Stock</DialogTitle>
          <DialogDescription>
            The field below is pre-filled with the distributor&apos;s counted quantity. Edit as needed
            — the system stock will be set to this exact value.
          </DialogDescription>
        </DialogHeader>
        <Form {...form}>
          <form
            onSubmit={form.handleSubmit((data) => {
              if (!selectedLineId) return
              mutate({ lineId: selectedLineId, data })
            })}
            className="space-y-4"
          >
            <FormField
              control={form.control}
              name="adjustedQuantity"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Adjusted Quantity</FormLabel>
                  <FormControl>
                    <Input
                      type="number"
                      min={0}
                      step="0.0001"
                      {...field}
                      onChange={(e) => field.onChange(parseFloat(e.target.value) || 0)}
                    />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />
            <Button type="submit" className="w-full" disabled={isPending}>
              {isPending ? <Spinner className="mr-2" /> : null}
              Apply Adjustment
            </Button>
          </form>
        </Form>
      </DialogContent>
    </Dialog>
  )
}

// --- Combined export ---

export function StockTakingDialogs() {
  return (
    <>
      <CreatePeriodDialog />
      <LockPeriodDialog />
      <UnlockPeriodDialog />
      <AdjustLineDialog />
    </>
  )
}
