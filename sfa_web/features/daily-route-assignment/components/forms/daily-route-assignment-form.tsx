'use client'

import { useEffect, useCallback, useState } from 'react'
import { useForm, Controller } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { ArrowDown } from 'lucide-react'
import { AsyncSelect } from '@/components/async-select'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Spinner } from '@/components/ui/spinner'
import {
  Form,
  FormControl,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from '@/components/ui/form'
import {
  createDailyRouteAssignmentSchema,
  type CreateDailyRouteAssignmentInput,
  type RepRouteDto,
  type SupervisorRepDto,
} from '../../schema/daily-route-assignment.schema'
import { useSupervisorsForSelect, useSupervisorReps, useRepRoutes } from '../../hooks/daily-route-assignment.hooks'
import type { UserDto } from '@/features/user/schema/user.schema'

const roleBadgeClass: Record<string, string> = {
  NSM: 'bg-blue-100 text-blue-700 border-blue-200',
  RSM: 'bg-purple-100 text-purple-700 border-purple-200',
  ASM: 'bg-indigo-100 text-indigo-700 border-indigo-200',
  Supervisor: 'bg-orange-100 text-orange-700 border-orange-200',
  SalesRep: 'bg-green-100 text-green-700 border-green-200',
  Admin: 'bg-red-100 text-red-700 border-red-200',
}

function getInitials(name: string) {
  return name
    .split(' ')
    .slice(0, 2)
    .map((n) => n[0])
    .join('')
    .toUpperCase()
}

function RoleBadge({ role }: { role: string }) {
  const cls = roleBadgeClass[role] ?? 'bg-muted text-muted-foreground border-border'
  return (
    <Badge variant="outline" className={`text-xs font-medium ${cls}`}>
      {role}
    </Badge>
  )
}

function PersonPreviewCard({ name, role }: { name: string; role: string }) {
  return (
    <div className="flex items-center gap-3 rounded-lg border bg-muted/40 px-4 py-3">
      <div className="flex h-9 w-9 shrink-0 items-center justify-center rounded-full bg-primary/10 text-sm font-semibold text-primary">
        {getInitials(name)}
      </div>
      <div>
        <p className="text-sm font-medium leading-none">{name}</p>
        <div className="mt-1">
          <RoleBadge role={role} />
        </div>
      </div>
    </div>
  )
}

function PersonOption({ name, role }: { name: string; role: string }) {
  return (
    <div className="flex items-center gap-2 py-0.5">
      <div className="flex h-6 w-6 shrink-0 items-center justify-center rounded-full bg-muted text-[10px] font-semibold text-muted-foreground">
        {getInitials(name)}
      </div>
      <div className="flex flex-col">
        <span className="text-sm leading-none">{name}</span>
        <span className="text-xs text-muted-foreground">{role}</span>
      </div>
    </div>
  )
}

function RoutePreviewCard({ name }: { name: string }) {
  return (
    <div className="flex items-center gap-3 rounded-lg border bg-muted/40 px-4 py-3">
      <div className="flex h-9 w-9 shrink-0 items-center justify-center rounded-full bg-primary/10 text-sm font-semibold text-primary">
        RT
      </div>
      <p className="text-sm font-medium leading-none">{name}</p>
    </div>
  )
}

const SUPERVISOR_NO_RESULTS = 'No supervisors found'
const REP_NO_RESULTS = 'No sales reps for this supervisor'
const ROUTE_NO_RESULTS = 'No routes available for this rep'

interface DailyRouteAssignmentFormProps {
  onSubmit: (data: CreateDailyRouteAssignmentInput) => void
  onCancel?: () => void
  isLoading: boolean
  fieldErrors?: Record<string, string> | null
}

export function DailyRouteAssignmentForm({
  onSubmit,
  onCancel,
  isLoading,
  fieldErrors,
}: DailyRouteAssignmentFormProps) {
  const { data: supervisors = [], isLoading: isLoadingSupervisors } = useSupervisorsForSelect()
  const [supervisorId, setSupervisorId] = useState(0)

  const { data: reps = [], isFetching: isLoadingReps } = useSupervisorReps(supervisorId)

  const form = useForm<CreateDailyRouteAssignmentInput>({
    resolver: zodResolver(createDailyRouteAssignmentSchema),
    defaultValues: {
      userId: 0,
      routeId: 0,
      assignedDate: new Date().toISOString().split('T')[0],
    },
  })

  const { setError, setValue, watch } = form
  const userId = watch('userId')
  const routeId = watch('routeId')

  const { data: repRoutes = [], isFetching: isLoadingRepRoutes } = useRepRoutes(userId)

  const selectedSupervisor = supervisors.find((u) => u.id === supervisorId)
  const selectedRep = reps.find((r) => r.userId === userId)
  const selectedRoute = repRoutes.find((r) => r.routeId === routeId)

  const supervisorFetcher = useCallback(
    async (query?: string): Promise<UserDto[]> => {
      const pool = supervisors.filter((u) => u.role === 'Supervisor' && u.isActive)
      if (!query) return pool
      return pool.filter((u) => u.name.toLowerCase().includes(query.toLowerCase()))
    },
    [supervisors],
  )

  const repFetcher = useCallback(
    async (query?: string): Promise<SupervisorRepDto[]> => {
      const pool = reps.filter((r) => r.userRole === 'SalesRep' && r.isActive)
      if (!query) return pool
      return pool.filter((r) => r.userName.toLowerCase().includes(query.toLowerCase()))
    },
    [reps],
  )

  const routeFetcher = useCallback(
    async (query?: string): Promise<RepRouteDto[]> => {
      if (!query) return repRoutes
      return repRoutes.filter((r) => r.routeName.toLowerCase().includes(query.toLowerCase()))
    },
    [repRoutes],
  )

  function handleSupervisorChange(value: string) {
    const id = value ? Number(value) : 0
    setSupervisorId(id)
    setValue('userId', 0)
    setValue('routeId', 0)
  }

  function handleRepChange(value: string) {
    setValue('userId', value ? Number(value) : 0)
    setValue('routeId', 0)
  }

  useEffect(() => {
    if (fieldErrors) {
      Object.entries(fieldErrors).forEach(([field, message]) => {
        setError(field as keyof CreateDailyRouteAssignmentInput, { message })
      })
    }
  }, [fieldErrors, setError])

  return (
    <Form {...form}>
      <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
        {/* Supervisor section */}
        <div className="space-y-2">
          <p className="text-xs font-semibold uppercase tracking-wide text-muted-foreground">
            Supervisor <span className="text-destructive">*</span>
          </p>
          <AsyncSelect<UserDto>
            fetcher={supervisorFetcher}
            preload={false}
            label="supervisor"
            placeholder="Select supervisor…"
            value={supervisorId > 0 ? String(supervisorId) : ''}
            onChange={handleSupervisorChange}
            getOptionValue={(u) => String(u.id)}
            getDisplayValue={(u) => <span>{u.name}</span>}
            renderOption={(u) => <PersonOption name={u.name} role={u.role} />}
            noResultsMessage={SUPERVISOR_NO_RESULTS}
            disabled={isLoadingSupervisors}
            width="100%"
            triggerClassName="w-full"
          />
          {selectedSupervisor && (
            <PersonPreviewCard name={selectedSupervisor.name} role={selectedSupervisor.role} />
          )}
        </div>

        <div className="flex justify-center">
          <div className="flex h-8 w-8 items-center justify-center rounded-full border bg-muted text-muted-foreground">
            <ArrowDown className="h-4 w-4" />
          </div>
        </div>

        {/* Sales rep section */}
        <div className="space-y-2">
          <p className="text-xs font-semibold uppercase tracking-wide text-muted-foreground">
            Sales Rep <span className="text-destructive">*</span>
          </p>
          <Controller
            control={form.control}
            name="userId"
            render={({ field, fieldState }) => (
              <div className="space-y-1">
                <AsyncSelect<SupervisorRepDto>
                  key={supervisorId}
                  fetcher={repFetcher}
                  preload={false}
                  label="sales rep"
                  placeholder={supervisorId > 0 ? 'Select sales rep…' : 'Select a supervisor first'}
                  value={field.value > 0 ? String(field.value) : ''}
                  onChange={handleRepChange}
                  getOptionValue={(r) => String(r.userId)}
                  getDisplayValue={(r) => <span>{r.userName}</span>}
                  renderOption={(r) => <PersonOption name={r.userName} role={r.userRole} />}
                  noResultsMessage={REP_NO_RESULTS}
                  disabled={supervisorId === 0 || isLoadingReps}
                  width="100%"
                  triggerClassName="w-full"
                />
                {fieldState.error && (
                  <p className="text-xs text-destructive">{fieldState.error.message}</p>
                )}
              </div>
            )}
          />
          {selectedRep && <PersonPreviewCard name={selectedRep.userName} role={selectedRep.userRole} />}
        </div>

        <div className="flex justify-center">
          <div className="flex h-8 w-8 items-center justify-center rounded-full border bg-muted text-muted-foreground">
            <ArrowDown className="h-4 w-4" />
          </div>
        </div>

        {/* Route section */}
        <div className="space-y-2">
          <p className="text-xs font-semibold uppercase tracking-wide text-muted-foreground">
            Route <span className="text-destructive">*</span>
          </p>
          <Controller
            control={form.control}
            name="routeId"
            render={({ field, fieldState }) => (
              <div className="space-y-1">
                <AsyncSelect<RepRouteDto>
                  key={userId}
                  fetcher={routeFetcher}
                  preload={false}
                  label="route"
                  placeholder={userId > 0 ? 'Select route…' : 'Select a sales rep first'}
                  value={field.value > 0 ? String(field.value) : ''}
                  onChange={(v) => field.onChange(v ? Number(v) : 0)}
                  getOptionValue={(r) => String(r.routeId)}
                  getDisplayValue={(r) => <span>{r.routeName}</span>}
                  renderOption={(r) => <span className="text-sm">{r.routeName}</span>}
                  noResultsMessage={ROUTE_NO_RESULTS}
                  disabled={userId === 0 || isLoadingRepRoutes}
                  width="100%"
                  triggerClassName="w-full"
                />
                {fieldState.error && (
                  <p className="text-xs text-destructive">{fieldState.error.message}</p>
                )}
              </div>
            )}
          />
          {selectedRoute && <RoutePreviewCard name={selectedRoute.routeName} />}
        </div>

        {/* Assigned date */}
        <FormField
          control={form.control}
          name="assignedDate"
          render={({ field }) => (
            <FormItem>
              <FormLabel className="text-xs font-semibold uppercase tracking-wide text-muted-foreground">
                Assigned Date <span className="text-destructive">*</span>
              </FormLabel>
              <FormControl>
                <Input type="date" className="w-full" {...field} />
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />

        <div className="flex justify-end gap-3 pt-1">
          {onCancel && (
            <Button type="button" variant="outline" onClick={onCancel} disabled={isLoading}>
              Cancel
            </Button>
          )}
          <Button
            type="submit"
            disabled={isLoading || isLoadingSupervisors}
            className="bg-orange-500 hover:bg-orange-600 text-white"
          >
            {isLoading ? <Spinner className="mr-2" /> : null}
            Assign route
          </Button>
        </div>
      </form>
    </Form>
  )
}
