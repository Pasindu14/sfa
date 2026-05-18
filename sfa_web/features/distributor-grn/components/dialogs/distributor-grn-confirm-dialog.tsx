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
import { useConfirmMyGrn } from '../../hooks/distributor-grn.hooks'

interface Props {
  grnId: number | null
  onClose: () => void
}

export function DistributorGrnConfirmDialog({ grnId, onClose }: Props) {
  const [receivedAt, setReceivedAt] = useState<Date | undefined>(new Date())
  const [notes, setNotes] = useState('')
  const [calendarOpen, setCalendarOpen] = useState(false)

  const { mutate, isPending, fieldErrors } = useConfirmMyGrn(onClose)

  function handleConfirm() {
    if (!grnId || !receivedAt) return
    mutate({
      id: grnId,
      data: {
        receivedAt: receivedAt.toISOString(),
        notes: notes.trim() || undefined,
      },
    })
  }

  function handleOpenChange(open: boolean) {
    if (!open) {
      onClose()
      setReceivedAt(new Date())
      setNotes('')
    }
  }

  return (
    <Dialog open={grnId !== null} onOpenChange={handleOpenChange}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle>Confirm GRN Receipt</DialogTitle>
          <DialogDescription>
            Mark this GRN as received. Enter the date goods were received and any notes.
          </DialogDescription>
        </DialogHeader>

        <div className="space-y-4 py-2">
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
          <Button variant="outline" onClick={onClose} disabled={isPending}>
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
