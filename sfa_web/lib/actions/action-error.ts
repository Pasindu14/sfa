// lib/actions/action-error.ts
//
// Client-safe (no server imports). Server actions can't throw across the RSC boundary, so
// they return ActionResponse and query/mutation functions rethrow `{ success: false }` as an
// Error. A plain `new Error(result.error)` loses the HTTP status and error code, which the
// QueryClient's retry policy needs to tell a permanent 4xx from a transient 5xx/network blip.

type FailureLike = {
  error?: string
  code?: string
  status?: number
}

/**
 * Error thrown from a hook when a server action returns `{ success: false }`.
 *
 * Behaves exactly like the `new Error(result.error)` it replaces — same `message`, same
 * `name` ("Error"), still `instanceof Error`. The diagnostic fields are deliberately NOT named
 * `code` / `status` / `fields`: `handleErrorToast()` and the form hooks switch on those names,
 * and exposing them here would silently change which toast (or sign-out) an existing
 * `onError` triggers.
 */
export class ActionError extends Error {
  /** HTTP status from the API, when the failure came from an ApiError. */
  public readonly httpStatus?: number
  /** Error code from the action result (e.g. NOT_FOUND, VALIDATION_ERROR). */
  public readonly errorCode?: string

  constructor(result: FailureLike) {
    super(result.error)
    this.httpStatus = typeof result.status === 'number' ? result.status : undefined
    this.errorCode = typeof result.code === 'string' ? result.code : undefined
  }
}
