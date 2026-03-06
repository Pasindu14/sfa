'use client'

import { useCallback } from 'react'
import { toast } from 'sonner'
import { useRouter } from 'next/navigation'
import { signOut } from '@/auth'

/**
 * Centralized error handling with toast notifications
 * Handles different error codes from server actions
 */
export function useErrorToast() {
  const router = useRouter()

  const showError = useCallback((error: string, code?: string) => {
    // Handle specific error codes
    switch (code) {
      case 'SESSION_EXPIRED':
        toast.error('Session expired. Please log in again.')
        // Sign out the user and redirect to login
        signOut({ redirect: false }).then(() => {
          router.push('/login')
          router.refresh()
        })
        break
      case 'VALIDATION_ERROR':
        toast.error(`Validation Error: ${error}`)
        break
      case 'UNAUTHORIZED':
        toast.error('Please log in to continue')
        break
      case 'FORBIDDEN':
        toast.error('You do not have permission to perform this action')
        break
      case 'NOT_FOUND':
        toast.error('The requested item was not found')
        break
      case 'CONFLICT':
        toast.error(`Conflict: ${error}`)
        break
      case 'INTERNAL_ERROR':
        toast.error('An unexpected error occurred. Please try again.')
        break
      default:
        toast.error(error || 'An error occurred')
    }
  }, [router])

  const showSuccess = useCallback((message: string) => {
    toast.success(message)
  }, [])

  const showInfo = useCallback((message: string) => {
    toast.info(message)
  }, [])

  const showWarning = useCallback((message: string) => {
    toast.warning(message)
  }, [])

  return {
    showError,
    showSuccess,
    showInfo,
    showWarning,
  }
}
