'use client'

import { useState } from 'react'
import { queryOptions, useQuery, useMutation, useQueryClient, keepPreviousData } from '@tanstack/react-query'
import { toast } from 'sonner'
import {
  getUsersAction,
  getUserByIdAction,
  createUserAction,
  updateUserAction,
  deleteUserAction,
  changePasswordAction,
  activateUserAction,
  deactivateUserAction,
} from '../actions/user.actions'
import {
  useCreateDialog,
  useEditDialog,
  useDeleteDialog,
  useChangePasswordDialog,
  useActivateDialog,
  useDeactivateDialog,
} from '../store'
import { handleErrorToast } from '@/lib/hooks/use-error-toast'
import type { CreateUserInput, UpdateUserInput, ChangePasswordInput } from '../schema/user.schema'

// --- Query key factory ---

export const userKeys = {
  all: ['users'] as const,
  lists: () => [...userKeys.all, 'list'] as const,
  list: (filters: object) => [...userKeys.lists(), filters] as const,
  details: () => [...userKeys.all, 'detail'] as const,
  detail: (id: number) => [...userKeys.details(), id] as const,
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
) {
  return useQuery({
    queryKey: userKeys.list({ page, pageSize, search }),
    queryFn: async () => {
      const result = await getUsersAction(page, pageSize)
      if (!result.success) throw new Error(result.error)
      const { users, page: p, pageSize: ps, totalCount } = result.data
      const term = search.trim().toLowerCase()
      const filtered = term
        ? users.filter(
            (u) =>
              u.name.toLowerCase().includes(term) ||
              u.username.toLowerCase().includes(term) ||
              u.email.toLowerCase().includes(term) ||
              u.phone.toLowerCase().includes(term) ||
              u.role.toLowerCase().includes(term)
          )
        : users
      return {
        success: true as const,
        data: filtered,
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

;(useUserDataTable as any).isQueryHook = true

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
      queryClient.invalidateQueries({ queryKey: userKeys.all })
      setFieldErrors(null)
      close()
      toast.success('User created successfully')
    },
    onError: (error: any) => {
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

      console.log(result)
      if (!result.success) throw result
      return result.data
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: userKeys.all })
      setFieldErrors(null)
      close()
      toast.success('User updated successfully')
    },
    onError: (error: any) => {
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
      queryClient.invalidateQueries({ queryKey: userKeys.all })
      close()
      toast.success('User deleted successfully')
    },
    onError: (error: any) => {
      handleErrorToast(error, 'user', 'delete')
    },
  })
}

export function useChangePassword() {
  const { close } = useChangePasswordDialog()
  const [fieldErrors, setFieldErrors] = useState<Record<string, string> | null>(null)

  const mutation = useMutation({
    mutationFn: async ({ id, data }: { id: number; data: ChangePasswordInput }) => {
      const result = await changePasswordAction(id, data)
      if (!result.success) throw result
      return result.data
    },
    onSuccess: () => {
      setFieldErrors(null)
      close()
      toast.success('Password changed successfully')
    },
    onError: (error: any) => {
      if (error.fields) setFieldErrors(error.fields)
      handleErrorToast(error, 'user', 'change password')
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
      queryClient.invalidateQueries({ queryKey: userKeys.all })
      close()
      toast.success('User activated successfully')
    },
    onError: (error: any) => {
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
      queryClient.invalidateQueries({ queryKey: userKeys.all })
      close()
      toast.success('User deactivated successfully')
    },
    onError: (error: any) => {
      handleErrorToast(error, 'user', 'deactivate')
    },
  })
}
