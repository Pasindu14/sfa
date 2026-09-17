// lib/auth/session.ts
import { cache } from 'react'
import { auth } from '@/auth'

/**
 * Request-scoped `auth()`.
 *
 * Both the action wrapper (role check) and the axios request interceptor (Bearer token) need
 * the session, and a page render can fire several API calls. Each bare `auth()` decodes the
 * session cookie and runs the jwt callback again. React `cache` collapses them onto the first
 * call's promise for the lifetime of one server request.
 *
 * Token refresh: if that first call's jwt callback refreshes the access token, the resolved
 * session already holds the refreshed token, so every later caller in the same request gets
 * it too — and the refresh token is spent at most once per request (the in-process
 * single-flight in auth.ts still covers concurrent requests).
 *
 * Scope: React only memoizes while a React server request is active (RSC render). Anywhere
 * else — server actions, route handlers, scripts — `cache` has no request store and simply
 * calls `auth()` every time, i.e. exactly the previous behaviour. The middleware/proxy uses
 * its own edge NextAuth instance and never imports this module.
 */
export const getSession = cache(() => auth())
