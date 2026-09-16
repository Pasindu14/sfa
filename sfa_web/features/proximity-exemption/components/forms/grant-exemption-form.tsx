'use client'

import { useCallback, useEffect } from 'react'
import { useForm, Controller } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { AsyncSelect } from '@/components/async-select'
import { getUsersAction } from '@/features/user/actions/user.actions'
import type { UserDto } from '@/features/user/schema/user.schema'
import {
  grantExemptionSchema,
  grantExemptionWithRepSchema,
  exemptionReasonEnum,
  exemptionReasonLabels,
  MAX_EXEMPTION_DAYS,
  type GrantExemptionWithRepInput,
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
  onSubmit: (data: GrantExemptionWithRepInput) => void
  isLoading: boolean
  fieldErrors?: Record<string, string> | null
  /// When true the form picks the rep itself — used on the Proximity Exemptions
  /// page, where there is no row to imply one. The Users-page dialog leaves this
  /// off because the rep is already known and re-asking would invite picking the
  /// wrong one.
  withRepPicker?: boolean
}

/// Searches active sales reps only. Both filters are applied server-side: `role`
/// so managers and distributors never appear (the geofence does not apply to
/// them, and the API would reject the grant anyway), and `isActive` so a
/// deactivated rep cannot be handed an exemption they can never use.
function useSalesRepFetcher() {
  return useCallback(async (query?: string): Promise<UserDto[]> => {
    const result = await getUsersAction(1, 50, query?.trim() || undefined, 'SalesRep', true)
    if (!result.success) return []
    return result.data.users
  }, [])
}

function RepOption({ user }: { user: UserDto }) {
  return (
    <div className="flex flex-col">
      <span className="text-sm font-medium">{user.name}</span>
      <span className="text-muted-foreground text-xs">@{user.username}</span>
    </div>
  )
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
  withRepPicker = false,
}: GrantExemptionFormProps) {
  const salesRepFetcher = useSalesRepFetcher()

  const form = useForm<GrantExemptionWithRepInput>({
    resolver: zodResolver(
      (withRepPicker
        ? grantExemptionWithRepSchema
        : grantExemptionSchema) as typeof grantExemptionWithRepSchema,
    ),
    defaultValues: {
      userId: 0,
      validUntil: colomboToday(),
      reason: 'BadOutletCoordinates',
      notes: '',
    },
  })

  const { setError } = form

  useEffect(() => {
    if (fieldErrors) {
      Object.entries(fieldErrors).forEach(([field, message]) => {
        setError(field as keyof GrantExemptionWithRepInput, { message })
      })
    }
  }, [fieldErrors, setError])

  return (
    <Form {...form}>
      <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
        {withRepPicker && (
          <Controller
            control={form.control}
            name="userId"
            render={({ field, fieldState }) => (
              <div className="space-y-1">
                <AsyncSelect<UserDto>
                  fetcher={salesRepFetcher}
                  preload={false}
                  label="Sales Rep"
                  placeholder="Type to search a sales rep…"
                  value={field.value > 0 ? String(field.value) : ''}
                  onChange={(v) => field.onChange(v ? Number(v) : 0)}
                  getOptionValue={(u) => String(u.id)}
                  getDisplayValue={(u) => <span>{u.name}</span>}
                  renderOption={(u) => <RepOption user={u} />}
                  noResultsMessage="No active sales rep matches that search."
                  // AsyncSelect writes width as an inline style (default 200px),
                  // which outranks any Tailwind class — so w-full does nothing
                  // here and the width has to come through this prop.
                  width="100%"
                  clearable
                />
                {fieldState.error && (
                  <p className="text-destructive text-sm">{fieldState.error.message}</p>
                )}
              </div>
            )}
          />
        )}

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
