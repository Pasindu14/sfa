'use client'

import { useState } from 'react'
import { queryOptions, useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
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

// --- Mutation hooks ---

export function useCreateUser() {
  const queryClient = useQueryClient()
  const { close } = useCreateDialog()
  const [fieldErrors, setFieldErrors] = useState<Record<string, string> | null>(null)

  const mutation = useMutation({
    mutationFn: (data: CreateUserInput) => createUserAction(data),
    onSuccess: (result) => {
      if (!result.success) {
        toast.error(result.error)
        if (result.fields) setFieldErrors(result.fields)
        return
      }
      queryClient.invalidateQueries({ queryKey: userKeys.lists() })
      setFieldErrors(null)
      close()
      toast.success('User created successfully')
    },
    onError: () => toast.error('Failed to create user'),
  })

  return { ...mutation, fieldErrors, clearFieldErrors: () => setFieldErrors(null) }
}

export function useUpdateUser() {
  const queryClient = useQueryClient()
  const { close } = useEditDialog()
  const [fieldErrors, setFieldErrors] = useState<Record<string, string> | null>(null)

  const mutation = useMutation({
    mutationFn: ({ id, data }: { id: number; data: UpdateUserInput }) =>
      updateUserAction(id, data),
    onSuccess: (result) => {
      if (!result.success) {
        toast.error(result.error)
        if (result.fields) setFieldErrors(result.fields)
        return
      }
      queryClient.invalidateQueries({ queryKey: userKeys.lists() })
      queryClient.invalidateQueries({ queryKey: userKeys.detail(result.data.id) })
      setFieldErrors(null)
      close()
      toast.success('User updated successfully')
    },
    onError: () => toast.error('Failed to update user'),
  })

  return { ...mutation, fieldErrors, clearFieldErrors: () => setFieldErrors(null) }
}

export function useDeleteUser() {
  const queryClient = useQueryClient()
  const { close } = useDeleteDialog()

  return useMutation({
    mutationFn: (id: number) => deleteUserAction(id),
    onSuccess: (result) => {
      if (!result.success) {
        toast.error(result.error)
        return
      }
      queryClient.invalidateQueries({ queryKey: userKeys.lists() })
      close()
      toast.success('User deleted successfully')
    },
    onError: () => toast.error('Failed to delete user'),
  })
}

export function useChangePassword() {
  const { close } = useChangePasswordDialog()
  const [fieldErrors, setFieldErrors] = useState<Record<string, string> | null>(null)

  const mutation = useMutation({
    mutationFn: ({ id, data }: { id: number; data: ChangePasswordInput }) =>
      changePasswordAction(id, data),
    onSuccess: (result) => {
      if (!result.success) {
        toast.error(result.error)
        if (result.fields) setFieldErrors(result.fields)
        return
      }
      setFieldErrors(null)
      close()
      toast.success('Password changed successfully')
    },
    onError: () => toast.error('Failed to change password'),
  })

  return { ...mutation, fieldErrors, clearFieldErrors: () => setFieldErrors(null) }
}

export function useActivateUser() {
  const queryClient = useQueryClient()
  const { close } = useActivateDialog()

  return useMutation({
    mutationFn: (id: number) => activateUserAction(id),
    onSuccess: (result) => {
      if (!result.success) {
        toast.error(result.error)
        return
      }
      queryClient.invalidateQueries({ queryKey: userKeys.lists() })
      queryClient.invalidateQueries({ queryKey: userKeys.detail(result.data.id) })
      close()
      toast.success('User activated successfully')
    },
    onError: () => toast.error('Failed to activate user'),
  })
}

export function useDeactivateUser() {
  const queryClient = useQueryClient()
  const { close } = useDeactivateDialog()

  return useMutation({
    mutationFn: (id: number) => deactivateUserAction(id),
    onSuccess: (result) => {
      if (!result.success) {
        toast.error(result.error)
        return
      }
      queryClient.invalidateQueries({ queryKey: userKeys.lists() })
      queryClient.invalidateQueries({ queryKey: userKeys.detail(result.data.id) })
      close()
      toast.success('User deactivated successfully')
    },
    onError: () => toast.error('Failed to deactivate user'),
  })
}
