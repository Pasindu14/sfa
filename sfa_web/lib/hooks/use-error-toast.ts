import { toast } from 'sonner'
import { signOut } from 'next-auth/react'
import type { ActionFailure } from '@/lib/types/actions'

/**
 * Display error toast based on error code
 * Provides consistent error messaging across all hooks
 * 
 * @example
 * handleErrorToast(error, 'product', 'create')
 * handleErrorToast(error, 'category', 'delete')
 */
export function handleErrorToast(
  error: ActionFailure,
  resource: string = 'resource',
  action: string = 'perform action'
) {
  const resourceCapitalized = resource.charAt(0).toUpperCase() + resource.slice(1)
  console.error('[handleErrorToast]', error)

  // Get the error message from ApiError.message or from data.error.message
  const errorMessage = error.error || 'An error occurred'

  // The error code + traceId are logged to the console above for engineers to
  // correlate with Seq — end users only ever see a plain human-readable message.

  switch (error.code) {
    case 'UNAUTHORIZED':
      toast.error('Session expired. Please log in again.')
      signOut({ redirectTo: '/sign-in' })
      break

    case 'VALIDATION_FAILED':
      if (error.fields && Object.keys(error.fields).length > 0) {
        toast.error(Object.values(error.fields).join('. '))
      } else {
        toast.error(errorMessage)
      }
      break
      
    case 'AUTH_TOKEN_EXPIRED':
      toast.error('Session expired. Please log in again.')
      break
      
    case 'AUTH_INVALID_TOKEN':
      toast.error('Invalid session. Please log in again.')
      break
      
    case 'FORBIDDEN_ACCESS':
      toast.error(errorMessage || `You do not have permission to access this ${resource}`)
      break
      
    case 'NOT_FOUND':
    case `${resource.toUpperCase()}_NOT_FOUND`:
      toast.error(errorMessage || `${resourceCapitalized} not found`)
      break
      
    case 'CONFLICT':
    case `${resource.toUpperCase()}_DUPLICATE`:
      toast.error(errorMessage || `${resourceCapitalized} already exists`)
      break
      
    case 'CONCURRENCY_CONFLICT':
      toast.error('Record was modified by another user. Please refresh and try again.')
      break
      
    case 'INSUFFICIENT_STOCK':
      toast.error(errorMessage || 'Insufficient stock available')
      break
      
    case 'INVALID_ORDER_STATE':
      toast.error(errorMessage || 'Invalid order state transition')
      break
      
    case 'LEAD_ALREADY_CONVERTED':
      toast.error(errorMessage || 'Lead has already been converted')
      break
      
    case 'RATE_LIMITED':
      toast.error('Too many requests. Please try again later.')
      break

    case 'SERVICE_UNAVAILABLE':
      toast.error(errorMessage || 'Service temporarily unavailable. Please try again later.')
      break

    case 'METHOD_NOT_ALLOWED':
      // A 405 is almost always a wiring bug (UI calling a verb/route the API doesn't expose),
      // not something the user did. Say so plainly.
      toast.error(`This action isn't available (${resourceCapitalized}).`)
      break

    case 'INTERNAL_ERROR':
      // Show the real message when the API gave us one instead of a hardcoded blank.
      toast.error(errorMessage || 'An unexpected error occurred. Please try again.')
      break

    default:
      toast.error(errorMessage || `Failed to ${action} ${resource}`)
  }
}