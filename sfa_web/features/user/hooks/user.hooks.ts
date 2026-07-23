'use client'

import { useState } from 'react'
import {
  queryOptions,
  useQuery,
  useMutation,
  useQueryClient,
  keepPreviousData,
  type QueryClient,
} from '@tanstack/react-query'
import { toast } from 'sonner'
import {
  getUsersAction,
  getUserByIdAction,
  createUserAction,
  updateUserAction,
  deleteUserAction,
  resetPasswordAction,
  activateUserAction,
  deactivateUserAction,
} from '../actions/user.actions'
import {
  useCreateDialog,
  useEditDialog,
  useDeleteDialog,
  useResetPasswordDialog,
  useActivateDialog,
  useDeactivateDialog,
} from '../store'
import { handleErrorToast } from '@/lib/hooks/use-error-toast'
import { allUserSelectKeys } from '@/lib/api/query-keys'
import type { ActionFailure } from '@/lib/types/actions'
import type { CreateUserInput, UpdateUserInput, ResetPasswordInput } from '../schema/user.schema'

// --- Query key factory ---

export const userKeys = {
  all: ['users'] as const,
  lists: () => [...userKeys.all, 'list'] as const,
  list: (filters: object) => [...userKeys.lists(), filters] as const,
  details: () => [...userKeys.all, 'detail'] as const,
  detail: (id: number) => [...userKeys.details(), id] as const,
}

// Other features cache their own copy of the user list to back AsyncSelect dropdowns
// (reporting lines, geo assignments). Those keys sit outside userKeys.all, so a
// deactivated user would linger in their dropdowns until the 5-minute stale time
// expired — invalidate them alongside the users list on every user mutation.
function invalidateUserCaches(queryClient: QueryClient) {
  queryClient.invalidateQueries({ queryKey: userKeys.all })
  allUserSelectKeys.forEach((queryKey) => queryClient.invalidateQueries({ queryKey }))
}

// --- Query options factory ---

export function userQueryOptions(page: number, pageSize: number) {
  return queryOptions({
    queryKey: userKeys.list({ page, pageSize }),
    queryFn: async () => {
      const result = await getUsersAction(page, pageSize)
      if (!result.success) throw new Error(result.error)
      return result.data
    },
  })
}

// --- Query hooks ---

export function useUsers(page: number, pageSize: number) {
  return useQuery(userQueryOptions(page, pageSize))
}

export function useUser(id: number | null) {
  return useQuery({
    queryKey: userKeys.detail(id!),
    queryFn: async () => {
      const result = await getUserByIdAction(id!)
      if (!result.success) throw new Error(result.error)
      return result.data
    },
    enabled: id !== null,
  })
}

// --- DataTable hook (used as fetchDataFn with isQueryHook = true) ---

export function useUserDataTable(
  page: number,
  pageSize: number,
  search: string,
  _dateRange?: { from_date: string; to_date: string },
  _sortBy?: string,
  _sortOrder?: string,
  _caseConfig?: unknown,
  customFilters?: { role?: string },
) {
  return useQuery({
    queryKey: userKeys.list({ page, pageSize, search, customFilters }),
    queryFn: async () => {
      const result = await getUsersAction(page, pageSize, search || undefined, customFilters?.role || undefined)
      if (!result.success) throw new Error(result.error)
      const { users, totalCount, page: p, pageSize: ps } = result.data
      return {
        success: true as const,
        data: users,
        pagination: {
          page: p,
          limit: ps,
          total_pages: Math.ceil(totalCount / ps),
          total_items: totalCount,
        },
      }
    },
    placeholderData: keepPreviousData,
  })
}

;(useUserDataTable as unknown as Record<string, unknown>).isQueryHook = true

// --- Mutation hooks ---

export function useCreateUser() {
  const queryClient = useQueryClient()
  const { close } = useCreateDialog()
  const [fieldErrors, setFieldErrors] = useState<Record<string, string> | null>(null)

  const mutation = useMutation({
    mutationFn: async (data: CreateUserInput) => {
      const result = await createUserAction(data)
      if (!result.success) throw result
      return result.data
    },
    onSuccess: () => {
      invalidateUserCaches(queryClient)
      setFieldErrors(null)
      close()
      toast.success('User created successfully')
    },
    onError: (error: ActionFailure) => {
      if (error.fields) setFieldErrors(error.fields)
      handleErrorToast(error, 'user', 'create')
    },
  })

  return { ...mutation, fieldErrors, clearFieldErrors: () => setFieldErrors(null) }
}

export function useUpdateUser() {
  const queryClient = useQueryClient()
  const { close } = useEditDialog()
  const [fieldErrors, setFieldErrors] = useState<Record<string, string> | null>(null)

  const mutation = useMutation({
    mutationFn: async ({ id, data }: { id: number; data: UpdateUserInput }) => {
      const result = await updateUserAction(id, data)
      if (!result.success) throw result
      return result.data
    },
    onSuccess: () => {
      invalidateUserCaches(queryClient)
      setFieldErrors(null)
      close()
      toast.success('User updated successfully')
    },
    onError: (error: ActionFailure) => {
      if (error.fields) setFieldErrors(error.fields)
      handleErrorToast(error, 'user', 'update')
    },
  })

  return { ...mutation, fieldErrors, clearFieldErrors: () => setFieldErrors(null) }
}

export function useDeleteUser() {
  const queryClient = useQueryClient()
  const { close } = useDeleteDialog()

  return useMutation({
    mutationFn: async (id: number) => {
      const result = await deleteUserAction(id)
      if (!result.success) throw result
    },
    onSuccess: () => {
      invalidateUserCaches(queryClient)
      close()
      toast.success('User deleted successfully')
    },
    onError: (error: ActionFailure) => {
      handleErrorToast(error, 'user', 'delete')
    },
  })
}

export function useResetPassword() {
  const { close } = useResetPasswordDialog()
  const [fieldErrors, setFieldErrors] = useState<Record<string, string> | null>(null)

  const mutation = useMutation({
    mutationFn: async ({ id, data }: { id: number; data: ResetPasswordInput }) => {
      const result = await resetPasswordAction(id, data)
      if (!result.success) throw result
      return result.data
    },
    onSuccess: () => {
      setFieldErrors(null)
      close()
      toast.success('Password reset successfully')
    },
    onError: (error: ActionFailure) => {
      if (error.fields) setFieldErrors(error.fields)
      handleErrorToast(error, 'user', 'reset password')
    },
  })

  return { ...mutation, fieldErrors, clearFieldErrors: () => setFieldErrors(null) }
}

export function useActivateUser() {
  const queryClient = useQueryClient()
  const { close } = useActivateDialog()

  return useMutation({
    mutationFn: async (id: number) => {
      const result = await activateUserAction(id)
      if (!result.success) throw result
    },
    onSuccess: () => {
      invalidateUserCaches(queryClient)
      close()
      toast.success('User activated successfully')
    },
    onError: (error: ActionFailure) => {
      handleErrorToast(error, 'user', 'activate')
    },
  })
}

export function useDeactivateUser() {
  const queryClient = useQueryClient()
  const { close } = useDeactivateDialog()

  return useMutation({
    mutationFn: async (id: number) => {
      const result = await deactivateUserAction(id)
      if (!result.success) throw result
    },
    onSuccess: () => {
      invalidateUserCaches(queryClient)
      close()
      toast.success('User deactivated successfully')
    },
    onError: (error: ActionFailure) => {
      handleErrorToast(error, 'user', 'deactivate')
    },
  })
}
