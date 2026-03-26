'use client'

import { useEffect, useState, useCallback } from 'react'
import { useForm, Controller } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { ArrowDown, CheckCircle2 } from 'lucide-react'
import { AsyncSelect } from '@/components/async-select'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Spinner } from '@/components/ui/spinner'
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
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from '@/components/ui/form'
import {
  createUserReportingLineSchema,
  updateUserReportingLineSchema,
  type CreateUserReportingLineInput,
  type UpdateUserReportingLineInput,
} from '../../schema/user-reporting-line.schema'
import { useUsersForSelect } from '../../hooks/user-reporting-line.hooks'
import type { UserDto } from '@/features/user/schema/user.schema'

const ASSIGNABLE_ROLES = ['NSM', 'RSM', 'ASM', 'Supervisor', 'SalesRep']
const MANAGER_ROLES = ['NSM', 'RSM', 'ASM', 'Supervisor']

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

function UserPreviewCard({ user }: { user: UserDto }) {
  return (
    <div className="flex items-center gap-3 rounded-lg border bg-muted/40 px-4 py-3">
      <div className="flex h-9 w-9 shrink-0 items-center justify-center rounded-full bg-primary/10 text-sm font-semibold text-primary">
        {getInitials(user.name)}
      </div>
      <div>
        <p className="text-sm font-medium leading-none">{user.name}</p>
        <div className="mt-1">
          <RoleBadge role={user.role} />
        </div>
      </div>
    </div>
  )
}

function UserOption({ user }: { user: UserDto }) {
  return (
    <div className="flex items-center gap-2 py-0.5">
      <div className="flex h-6 w-6 shrink-0 items-center justify-center rounded-full bg-muted text-[10px] font-semibold text-muted-foreground">
        {getInitials(user.name)}
      </div>
      <div className="flex flex-col">
        <span className="text-sm leading-none">{user.name}</span>
        <span className="text-xs text-muted-foreground">{user.role}</span>
      </div>
    </div>
  )
}

// --- Fetcher hooks (backed by TanStack Query cache — no extra API calls per keystroke) ---

function useSubordinateFetcher(users: UserDto[]) {
  return useCallback(
    async (query?: string): Promise<UserDto[]> => {
      if (!query) return []
      const pool = users.filter((u) => ASSIGNABLE_ROLES.includes(u.role) && u.isActive)
      return pool.filter((u) => u.name.toLowerCase().includes(query.toLowerCase()))
    },
    [users],
  )
}

function useManagerFetcher(users: UserDto[], role: string) {
  return useCallback(
    async (query?: string): Promise<UserDto[]> => {
      const pool = role
        ? users.filter((u) => u.role === role && u.isActive)
        : users.filter((u) => MANAGER_ROLES.includes(u.role) && u.isActive)
      // Show all immediately on open; filter when user types
      if (!query) return pool
      return pool.filter((u) => u.name.toLowerCase().includes(query.toLowerCase()))
    },
    [users, role],
  )
}

const SUBORDINATE_NO_RESULTS = 'Type to search…'
const MANAGER_NO_RESULTS = 'No users found'

// --- Interfaces ---

interface CreateFormProps {
  mode: 'create'
  defaultValues?: undefined
  onSubmit: (data: CreateUserReportingLineInput) => void
  onCancel?: () => void
  isLoading: boolean
  fieldErrors?: Record<string, string> | null
}

interface EditFormProps {
  mode: 'edit'
  defaultValues?: Partial<UpdateUserReportingLineInput> & { userName?: string; userRole?: string }
  onSubmit: (data: UpdateUserReportingLineInput) => void
  onCancel?: () => void
  isLoading: boolean
  fieldErrors?: Record<string, string> | null
}

type UserReportingLineFormProps = CreateFormProps | EditFormProps

// --- Main export ---

export function UserReportingLineForm(props: UserReportingLineFormProps) {
  const { data: users = [], isLoading: isLoadingUsers } = useUsersForSelect()

  if (props.mode === 'create') {
    return <CreateForm {...props} users={users} isLoadingUsers={isLoadingUsers} />
  }
  return <EditForm {...props} users={users} isLoadingUsers={isLoadingUsers} />
}

// --- Create form ---

function CreateForm({
  onSubmit,
  onCancel,
  isLoading,
  fieldErrors,
  users,
  isLoadingUsers,
}: Omit<CreateFormProps, 'mode'> & { users: UserDto[]; isLoadingUsers: boolean }) {
  const form = useForm<CreateUserReportingLineInput>({
    resolver: zodResolver(createUserReportingLineSchema),
    defaultValues: {
      userId: 0,
      reportsToUserId: 0,
      effectiveFrom: new Date().toISOString().split('T')[0],
    },
  })

  const { setError, setValue, watch } = form
  const userId = watch('userId')
  const reportsToUserId = watch('reportsToUserId')
  const [managerRole, setManagerRole] = useState('')

  const selectedUser = users.find((u) => u.id === userId)
  const selectedManager = users.find((u) => u.id === reportsToUserId)

  const subordinateFetcher = useSubordinateFetcher(users)
  const managerFetcher = useManagerFetcher(users, managerRole)

  function handleManagerRoleChange(role: string) {
    setManagerRole(role)
    setValue('reportsToUserId', 0) // reset user when role changes
  }

  useEffect(() => {
    if (fieldErrors) {
      Object.entries(fieldErrors).forEach(([field, message]) => {
        setError(field as keyof CreateUserReportingLineInput, { message })
      })
    }
  }, [fieldErrors, setError])

  return (
    <Form {...form}>
      <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">

        {/* Subordinate section */}
        <div className="space-y-2">
          <p className="text-xs font-semibold uppercase tracking-wide text-muted-foreground">
            Subordinate User <span className="text-destructive">*</span>
          </p>
          <Controller
            control={form.control}
            name="userId"
            render={({ field, fieldState }) => (
              <div className="space-y-1">
                <AsyncSelect<UserDto>
                  fetcher={subordinateFetcher}
                  preload={false}
                  label="user"
                  placeholder="Select user…"
                  value={field.value > 0 ? String(field.value) : ''}
                  onChange={(v) => field.onChange(v ? Number(v) : 0)}
                  getOptionValue={(u) => String(u.id)}
                  getDisplayValue={(u) => <span>{u.name}</span>}
                  renderOption={(u) => <UserOption user={u} />}
                  noResultsMessage={SUBORDINATE_NO_RESULTS}
                  disabled={isLoadingUsers}
                  width="100%"
                  triggerClassName="w-full"
                />
                {fieldState.error && (
                  <p className="text-xs text-destructive">{fieldState.error.message}</p>
                )}
              </div>
            )}
          />
          {selectedUser && <UserPreviewCard user={selectedUser} />}
        </div>

        {/* Arrow divider */}
        <div className="flex justify-center">
          <div className="flex h-8 w-8 items-center justify-center rounded-full border bg-muted text-muted-foreground">
            <ArrowDown className="h-4 w-4" />
          </div>
        </div>

        {/* Manager section */}
        <div className="space-y-3">
          <div className="space-y-1">
            <p className="text-xs font-semibold uppercase tracking-wide text-muted-foreground">
              Reports To <span className="text-destructive">*</span>
            </p>
            <div className="flex items-center gap-2">
              <span
                className={`inline-flex items-center gap-1 rounded-full px-2.5 py-0.5 text-xs font-medium transition-colors ${
                  managerRole
                    ? 'bg-primary/10 text-primary'
                    : 'bg-muted text-muted-foreground'
                }`}
              >
                {managerRole && <CheckCircle2 className="h-3 w-3" />}
                Step 1: Role
              </span>
              <div className="h-px w-4 bg-border" />
              <span
                className={`inline-flex items-center gap-1 rounded-full px-2.5 py-0.5 text-xs font-medium transition-colors ${
                  managerRole ? 'bg-orange-500 text-white' : 'bg-muted text-muted-foreground'
                }`}
              >
                Step 2: User
              </span>
            </div>
          </div>

          <div className="grid grid-cols-2 gap-3">
            {/* Step 1: role filter */}
            <div className="space-y-1">
              <p className="text-xs text-muted-foreground">Manager Role</p>
              <Select
                disabled={isLoadingUsers}
                value={managerRole}
                onValueChange={handleManagerRoleChange}
              >
                <SelectTrigger className="w-full">
                  <SelectValue placeholder="Select role" />
                </SelectTrigger>
                <SelectContent>
                  {MANAGER_ROLES.map((r) => (
                    <SelectItem key={r} value={r}>
                      {r}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>

            {/* Step 2: user filtered by role — key forces remount on role change */}
            <Controller
              control={form.control}
              name="reportsToUserId"
              render={({ field, fieldState }) => (
                <div className="space-y-1">
                  <p className="text-xs text-muted-foreground">Select User</p>
                  <AsyncSelect<UserDto>
                    key={managerRole}
                    fetcher={managerFetcher}
                    preload={false}
                    label="manager"
                    placeholder="Select user…"
                    value={field.value > 0 ? String(field.value) : ''}
                    onChange={(v) => field.onChange(v ? Number(v) : 0)}
                    getOptionValue={(u) => String(u.id)}
                    getDisplayValue={(u) => <span>{u.name}</span>}
                    renderOption={(u) => <UserOption user={u} />}
                    noResultsMessage={MANAGER_NO_RESULTS}
                    disabled={isLoadingUsers}
                    width="100%"
                    triggerClassName="w-full"
                  />
                  {fieldState.error && (
                    <p className="text-xs text-destructive">{fieldState.error.message}</p>
                  )}
                </div>
              )}
            />
          </div>

          {selectedManager && <UserPreviewCard user={selectedManager} />}
        </div>

        {/* Effective From */}
        <FormField
          control={form.control}
          name="effectiveFrom"
          render={({ field }) => (
            <FormItem>
              <FormLabel className="text-xs font-semibold uppercase tracking-wide text-muted-foreground">
                Effective From <span className="text-destructive">*</span>
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
            disabled={isLoading || isLoadingUsers}
            className="bg-orange-500 hover:bg-orange-600 text-white"
          >
            {isLoading ? <Spinner className="mr-2" /> : null}
            Save reporting line
          </Button>
        </div>
      </form>
    </Form>
  )
}

// --- Edit form ---

function EditForm({
  defaultValues,
  onSubmit,
  onCancel,
  isLoading,
  fieldErrors,
  users,
  isLoadingUsers,
}: Omit<EditFormProps, 'mode'> & { users: UserDto[]; isLoadingUsers: boolean }) {
  const existingManager = users.find((u) => u.id === defaultValues?.reportsToUserId)
  const [managerRole, setManagerRole] = useState(existingManager?.role ?? '')

  const form = useForm<UpdateUserReportingLineInput>({
    resolver: zodResolver(updateUserReportingLineSchema),
    defaultValues: {
      reportsToUserId: defaultValues?.reportsToUserId ?? 0,
      effectiveFrom: defaultValues?.effectiveFrom ?? new Date().toISOString().split('T')[0],
    },
  })

  const { setError, setValue, watch } = form
  const reportsToUserId = watch('reportsToUserId')
  const selectedManager = users.find((u) => u.id === reportsToUserId)

  const managerFetcher = useManagerFetcher(users, managerRole)

  // Sync manager role once users list loads (async after dialog open)
  useEffect(() => {
    if (existingManager && !managerRole) {
      setManagerRole(existingManager.role)
    }
  }, [existingManager, managerRole])

  function handleManagerRoleChange(role: string) {
    setManagerRole(role)
    setValue('reportsToUserId', 0)
  }

  useEffect(() => {
    if (fieldErrors) {
      Object.entries(fieldErrors).forEach(([field, message]) => {
        setError(field as keyof UpdateUserReportingLineInput, { message })
      })
    }
  }, [fieldErrors, setError])

  // Read-only subordinate preview
  const subordinateName = defaultValues?.userName ?? ''
  const subordinateRole = defaultValues?.userRole ?? ''
  const fakeSubordinate = subordinateName
    ? ({ id: 0, name: subordinateName, role: subordinateRole, isActive: true } as UserDto)
    : null

  return (
    <Form {...form}>
      <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">

        {/* Subordinate preview (read-only) */}
        {fakeSubordinate && (
          <div className="space-y-2">
            <p className="text-xs font-semibold uppercase tracking-wide text-muted-foreground">
              Subordinate User
            </p>
            <UserPreviewCard user={fakeSubordinate} />
          </div>
        )}

        {/* Arrow divider */}
        <div className="flex justify-center">
          <div className="flex h-8 w-8 items-center justify-center rounded-full border bg-muted text-muted-foreground">
            <ArrowDown className="h-4 w-4" />
          </div>
        </div>

        {/* Manager section */}
        <div className="space-y-3">
          <div className="space-y-1">
            <p className="text-xs font-semibold uppercase tracking-wide text-muted-foreground">
              Reports To <span className="text-destructive">*</span>
            </p>
            <div className="flex items-center gap-2">
              <span
                className={`inline-flex items-center gap-1 rounded-full px-2.5 py-0.5 text-xs font-medium transition-colors ${
                  managerRole ? 'bg-primary/10 text-primary' : 'bg-muted text-muted-foreground'
                }`}
              >
                {managerRole && <CheckCircle2 className="h-3 w-3" />}
                Step 1: Role
              </span>
              <div className="h-px w-4 bg-border" />
              <span
                className={`inline-flex items-center gap-1 rounded-full px-2.5 py-0.5 text-xs font-medium transition-colors ${
                  managerRole ? 'bg-orange-500 text-white' : 'bg-muted text-muted-foreground'
                }`}
              >
                Step 2: User
              </span>
            </div>
          </div>

          <div className="grid grid-cols-2 gap-3">
            <div className="space-y-1">
              <p className="text-xs text-muted-foreground">Manager Role</p>
              <Select
                disabled={isLoadingUsers}
                value={managerRole}
                onValueChange={handleManagerRoleChange}
              >
                <SelectTrigger className="w-full">
                  <SelectValue placeholder="Select role" />
                </SelectTrigger>
                <SelectContent>
                  {MANAGER_ROLES.map((r) => (
                    <SelectItem key={r} value={r}>
                      {r}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>

            <Controller
              control={form.control}
              name="reportsToUserId"
              render={({ field, fieldState }) => (
                <div className="space-y-1">
                  <p className="text-xs text-muted-foreground">Select User</p>
                  <AsyncSelect<UserDto>
                    key={managerRole}
                    fetcher={managerFetcher}
                    preload={false}
                    label="manager"
                    placeholder="Select user…"
                    value={field.value > 0 ? String(field.value) : ''}
                    onChange={(v) => field.onChange(v ? Number(v) : 0)}
                    getOptionValue={(u) => String(u.id)}
                    getDisplayValue={(u) => <span>{u.name}</span>}
                    renderOption={(u) => <UserOption user={u} />}
                    noResultsMessage={MANAGER_NO_RESULTS}
                    disabled={isLoadingUsers}
                    width="100%"
                    triggerClassName="w-full"
                    initialOption={existingManager ?? null}
                  />
                  {fieldState.error && (
                    <p className="text-xs text-destructive">{fieldState.error.message}</p>
                  )}
                </div>
              )}
            />
          </div>

          {selectedManager && <UserPreviewCard user={selectedManager} />}
        </div>

        {/* Effective From */}
        <FormField
          control={form.control}
          name="effectiveFrom"
          render={({ field }) => (
            <FormItem>
              <FormLabel className="text-xs font-semibold uppercase tracking-wide text-muted-foreground">
                Effective From <span className="text-destructive">*</span>
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
            disabled={isLoading || isLoadingUsers}
            className="bg-orange-500 hover:bg-orange-600 text-white"
          >
            {isLoading ? <Spinner className="mr-2" /> : null}
            Save reporting line
          </Button>
        </div>
      </form>
    </Form>
  )
}
