'use client'

import { useState } from 'react'
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
  DialogFooter,
} from '@/components/ui/dialog'
import { Button } from '@/components/ui/button'
import { Textarea } from '@/components/ui/textarea'
import { Label } from '@/components/ui/label'
import { Calendar } from '@/components/ui/calendar'
import { Popover, PopoverContent, PopoverTrigger } from '@/components/ui/popover'
import { CalendarIcon } from 'lucide-react'
import { format } from 'date-fns'
import { cn } from '@/lib/utils'
import { useConfirmDialog } from '../../store'
import { useConfirmGrn } from '../../hooks/grn.hooks'

export function GrnConfirmDialog() {
  const { isOpen, selectedGrnId, close } = useConfirmDialog()
  const { mutate, isPending, fieldErrors } = useConfirmGrn()

  const [receivedAt, setReceivedAt] = useState<Date | undefined>(new Date())
  const [notes, setNotes] = useState('')
  const [calendarOpen, setCalendarOpen] = useState(false)

  function handleConfirm() {
    if (!selectedGrnId || !receivedAt) return
    mutate({
      id: selectedGrnId,
      data: {
        receivedAt: receivedAt.toISOString(),
        notes: notes.trim() || undefined,
      },
    })
  }

  function handleOpenChange(open: boolean) {
    if (!open) {
      close()
      setReceivedAt(new Date())
      setNotes('')
    }
  }

  return (
    <Dialog open={isOpen} onOpenChange={handleOpenChange}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle>Confirm GRN Receipt</DialogTitle>
          <DialogDescription>
            Mark this GRN as received. Enter the date goods were received and any notes.
          </DialogDescription>
        </DialogHeader>

        <div className="space-y-4 py-2">
          {/* Received At */}
          <div className="space-y-2">
            <Label htmlFor="receivedAt">
              Received Date <span className="text-destructive">*</span>
            </Label>
            <Popover open={calendarOpen} onOpenChange={setCalendarOpen}>
              <PopoverTrigger asChild>
                <Button
                  id="receivedAt"
                  variant="outline"
                  className={cn(
                    'w-full justify-start text-left font-normal',
                    !receivedAt && 'text-muted-foreground'
                  )}
                >
                  <CalendarIcon className="mr-2 h-4 w-4" />
                  {receivedAt ? format(receivedAt, 'PPP') : 'Pick a date'}
                </Button>
              </PopoverTrigger>
              <PopoverContent className="w-auto p-0" align="start">
                <Calendar
                  mode="single"
                  selected={receivedAt}
                  onSelect={(date) => {
                    setReceivedAt(date)
                    setCalendarOpen(false)
                  }}
                  disabled={(date) => date > new Date()}
                  initialFocus
                />
              </PopoverContent>
            </Popover>
            {fieldErrors?.receivedAt && (
              <p className="text-xs text-destructive">{fieldErrors.receivedAt}</p>
            )}
          </div>

          {/* Notes */}
          <div className="space-y-2">
            <Label htmlFor="notes">Notes (optional)</Label>
            <Textarea
              id="notes"
              placeholder="Add any notes about this receipt..."
              value={notes}
              onChange={(e) => setNotes(e.target.value)}
              rows={3}
            />
          </div>
        </div>

        <DialogFooter>
          <Button variant="outline" onClick={close} disabled={isPending}>
            Cancel
          </Button>
          <Button
            onClick={handleConfirm}
            disabled={isPending || !receivedAt}
            className="bg-green-600 hover:bg-green-700"
          >
            {isPending ? 'Confirming...' : 'Confirm Receipt'}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
