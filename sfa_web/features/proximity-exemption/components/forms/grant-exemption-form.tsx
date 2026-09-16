'use client'

import { useEffect } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import {
  grantExemptionSchema,
  exemptionReasonEnum,
  exemptionReasonLabels,
  MAX_EXEMPTION_DAYS,
  type GrantExemptionInput,
} from '../../schema/proximity-exemption.schema'
import { toColomboDateStr } from '@/lib/utils/datetime'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Textarea } from '@/components/ui/textarea'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import {
  Form,
  FormControl,
  FormDescription,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from '@/components/ui/form'
import { Spinner } from '@/components/ui/spinner'

interface GrantExemptionFormProps {
  onSubmit: (data: GrantExemptionInput) => void
  isLoading: boolean
  fieldErrors?: Record<string, string> | null
}

/// Both bounds go through toColomboDateStr rather than `.toISOString().split('T')[0]`,
/// which is UTC-based and would offer "yesterday" as the earliest date to an admin
/// working before 05:30 Sri Lanka time.
function colomboToday(): string {
  return toColomboDateStr(new Date())
}

function colomboMaxDate(): string {
  const d = new Date()
  d.setDate(d.getDate() + MAX_EXEMPTION_DAYS)
  return toColomboDateStr(d)
}

export function GrantExemptionForm({
  onSubmit,
  isLoading,
  fieldErrors,
}: GrantExemptionFormProps) {
  const form = useForm<GrantExemptionInput>({
    resolver: zodResolver(grantExemptionSchema),
    defaultValues: {
      validUntil: colomboToday(),
      reason: 'BadOutletCoordinates',
      notes: '',
    },
  })

  const { setError } = form

  useEffect(() => {
    if (fieldErrors) {
      Object.entries(fieldErrors).forEach(([field, message]) => {
        setError(field as keyof GrantExemptionInput, { message })
      })
    }
  }, [fieldErrors, setError])

  return (
    <Form {...form}>
      <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
        <FormField
          control={form.control}
          name="validUntil"
          render={({ field }) => (
            <FormItem>
              <FormLabel>Applies through</FormLabel>
              <FormControl>
                <Input
                  type="date"
                  min={colomboToday()}
                  max={colomboMaxDate()}
                  {...field}
                />
              </FormControl>
              <FormDescription>
                The last day the rep can bill outside the geofence. Maximum{' '}
                {MAX_EXEMPTION_DAYS} days — grant a new one to extend.
              </FormDescription>
              <FormMessage />
            </FormItem>
          )}
        />

        <FormField
          control={form.control}
          name="reason"
          render={({ field }) => (
            <FormItem>
              <FormLabel>Reason</FormLabel>
              <Select onValueChange={field.onChange} value={field.value}>
                <FormControl>
                  <SelectTrigger>
                    <SelectValue placeholder="Choose a reason" />
                  </SelectTrigger>
                </FormControl>
                <SelectContent>
                  {exemptionReasonEnum.options.map((reason) => (
                    <SelectItem key={reason} value={reason}>
                      {exemptionReasonLabels[reason]}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
              <FormDescription>
                Reason codes make these grants reportable later — a run of
                &quot;outlet coordinates are wrong&quot; means the coordinates
                need fixing, not more exemptions.
              </FormDescription>
              <FormMessage />
            </FormItem>
          )}
        />

        <FormField
          control={form.control}
          name="notes"
          render={({ field }) => (
            <FormItem>
              <FormLabel>Notes (optional)</FormLabel>
              <FormControl>
                <Textarea
                  rows={3}
                  placeholder="Which outlets, who approved, anything the reason code cannot carry"
                  {...field}
                />
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />

        <Button type="submit" disabled={isLoading} className="w-full">
          {isLoading && <Spinner className="mr-2 size-4" />}
          Grant exemption
        </Button>
      </form>
    </Form>
  )
}
