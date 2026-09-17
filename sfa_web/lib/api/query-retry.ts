// lib/api/query-retry.ts
//
// Default retry policy for TanStack queries (client-safe — no server imports).

/** Retries after the first failure — i.e. at most 3 attempts in total. */
const MAX_QUERY_RETRIES = 2

/**
 * Error codes that mean "asking again will get the same answer". Used only when the HTTP
 * status is not known (e.g. an AppError / ZodError raised inside the action wrapper itself,
 * which carries a code but no status).
 */
const NON_RETRYABLE_CODES = new Set([
  // lib/actions/wrapper.ts + lib/errors.ts
  'VALIDATION_ERROR',
  'UNAUTHORIZED',
  'FORBIDDEN',
  'NOT_FOUND',
  'CONFLICT',
  'RATE_LIMIT',
  // API error codes / lib/api/client.ts status fallbacks
  'VALIDATION_FAILED',
  'AUTH_TOKEN_EXPIRED',
  'AUTH_INVALID_TOKEN',
  'FORBIDDEN_ACCESS',
  'METHOD_NOT_ALLOWED',
  'BUSINESS_RULE',
  'CONCURRENCY_CONFLICT',
  'RATE_LIMITED',
])

function readStatus(error: unknown): number | undefined {
  if (!error || typeof error !== 'object') return undefined
  const e = error as { httpStatus?: unknown; status?: unknown }
  if (typeof e.httpStatus === 'number') return e.httpStatus
  if (typeof e.status === 'number') return e.status
  return undefined
}

function readCode(error: unknown): string | undefined {
  if (!error || typeof error !== 'object') return undefined
  const e = error as { errorCode?: unknown; code?: unknown }
  if (typeof e.errorCode === 'string') return e.errorCode
  if (typeof e.code === 'string') return e.code
  return undefined
}

/**
 * Whether a failed query is worth retrying.
 * - 4xx: never (bad input, auth, not found — the next attempt returns the same).
 * - 5xx: yes.
 * - No status but a known client-error code: never.
 * - Anything else (network failure, server-action transport error, unknown): yes — the
 *   conservative behaviour this app has always had.
 */
export function isRetryableQueryError(error: unknown): boolean {
  const status = readStatus(error)
  if (status !== undefined) {
    if (status >= 400 && status < 500) return false
    return true
  }
  const code = readCode(error)
  if (code && NON_RETRYABLE_CODES.has(code)) return false
  return true
}

/** `retry` option for QueryClient defaultOptions.queries. */
export function queryRetry(failureCount: number, error: unknown): boolean {
  return failureCount < MAX_QUERY_RETRIES && isRetryableQueryError(error)
}
