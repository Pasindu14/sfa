import axios from "axios";
import https from "https";
import { auth } from "@/auth";

// ─────────────────────────────────────────────────────────────
// API contract types — mirrors C# records in Common/Errors/ApiResponse.cs
// ─────────────────────────────────────────────────────────────

/** Shape of every successful response: ApiResponse<T> */
export interface ApiSuccessBody<T = unknown> {
  success: true;
  data: T;
  pagination: {
    page: number;
    pageSize: number;
    total: number;
    totalPages: number;
  } | null;
  traceId: string;
}

/** Shape of every error body: ApiErrorResponse */
interface ApiErrorBody {
  success: false;
  error: {
    code: string;
    message: string;
    detail: string | null;
    /** Validation field errors — each key maps to one or more messages */
    fields: Record<string, string[]> | null;
    currentData: unknown | null;
    traceId: string;
    timestamp: string;
  };
}

// ─────────────────────────────────────────────────────────────
// ApiError — thrown by the response interceptor for every non-2xx
// ─────────────────────────────────────────────────────────────

export class ApiError extends Error {
  public readonly status: number;
  public readonly code: string;
  /** Field errors flattened to Record<field, "msg1, msg2"> for form binding */
  public readonly fields?: Record<string, string>;
  public readonly detail?: string;
  public readonly currentData?: unknown;
  public readonly traceId?: string;

  constructor(
    status: number,
    code: string,
    message: string,
    fields?: Record<string, string>,
    detail?: string,
    currentData?: unknown,
    traceId?: string
  ) {
    super(message);
    this.name = "ApiError";
    this.status = status;
    this.code = code;
    this.fields = fields;
    this.detail = detail;
    this.currentData = currentData;
    this.traceId = traceId;
    if (Error.captureStackTrace) {
      Error.captureStackTrace(this, this.constructor);
    }
  }
}

// ─────────────────────────────────────────────────────────────
// Idempotency key helper
// Callers generate a stable key BEFORE initiating a mutation and pass it
// via `config.headers["X-Idempotency-Key"]`.  The interceptor no longer
// sets a key globally — a per-request UUID defeats the purpose.
// ─────────────────────────────────────────────────────────────

/**
 * Generate a stable idempotency key for a mutation.
 * Call this once before the mutation and pass the result in the request
 * headers so that retries of the same logical operation reuse the key.
 *
 * @example
 * const idempotencyKey = createIdempotencyKey();
 * await client.post('/api/v1/orders', body, {
 *   headers: { 'X-Idempotency-Key': idempotencyKey },
 * });
 */
export function createIdempotencyKey(): string {
  return crypto.randomUUID();
}

// ─────────────────────────────────────────────────────────────
// Axios client
// ─────────────────────────────────────────────────────────────

const client = axios.create({
  baseURL: process.env.SFA_API_DOMAIN,
  headers: { "Content-Type": "application/json" },
  timeout: 30_000,
  httpsAgent: new https.Agent({
    rejectUnauthorized: process.env.NODE_ENV === "production",
  }),
});

// Methods whose requests mutate server state and therefore carry an idempotency key.
const MUTATING_METHODS = new Set(["post", "put", "patch", "delete"]);

// Attach Bearer token from Next-Auth session on every request
client.interceptors.request.use(async (config) => {
  const session = await auth();
  if (session?.user?.accessToken) {
    config.headers.Authorization = `Bearer ${session.user.accessToken}`;
  }

  // Attach a stable idempotency key to every mutating request. It is generated once and kept
  // on the config, so the transport-level retry below re-sends the SAME key — letting the API
  // collapse a duplicate (e.g. a request that committed but whose response was lost) instead of
  // creating a second record. Double-submit from the UI is separately prevented by disabling
  // submit buttons while the mutation is pending.
  if (config.method && MUTATING_METHODS.has(config.method.toLowerCase())) {
    if (!config.headers["X-Idempotency-Key"]) {
      config.headers["X-Idempotency-Key"] = createIdempotencyKey();
    }
  }

  return config;
});

// Normalise all non-2xx responses into ApiError
// For 401 responses with RefreshAccessTokenError, the session error field
// signals that the refresh failed and the user must re-authenticate.
// Client-side components should check `session.error === "RefreshAccessTokenError"`
// and call `signOut()` from `next-auth/react` to redirect to login.
client.interceptors.response.use(
  (response) => response,
  async (error) => {
    // Transport-level retry (once) for mutating requests. A missing response means the request
    // either never reached the server or its response was lost after the server committed.
    // Re-sending reuses the same X-Idempotency-Key (already on the config), so the API
    // deduplicates rather than creating a duplicate. Bounded to a single retry.
    if (axios.isAxiosError(error) && !error.response) {
      const retryConfig = error.config as
        | (NonNullable<typeof error.config> & { _idempotentRetry?: boolean })
        | undefined;
      if (
        retryConfig?.method &&
        MUTATING_METHODS.has(retryConfig.method.toLowerCase()) &&
        !retryConfig._idempotentRetry
      ) {
        retryConfig._idempotentRetry = true;
        return client(retryConfig);
      }
    }

    if (axios.isAxiosError(error) && error.response) {
      const status = error.response.status;
      const body = error.response.data as ApiErrorBody | undefined;
      const apiErr = body?.error;

      // Fallback message when the body is not our standard envelope
      const message =
        apiErr?.message ??
        error.message ??
        "An unexpected error occurred";

      const code =
        apiErr?.code ??
        (status === 401
          ? "UNAUTHORIZED"
          : status === 403
          ? "FORBIDDEN_ACCESS"
          : status === 404
          ? "NOT_FOUND"
          : status === 405
          ? "METHOD_NOT_ALLOWED"
          : status === 409
          ? "CONFLICT"
          : status === 422
          ? "BUSINESS_RULE"
          : status === 429
          ? "RATE_LIMITED"
          : status === 503
          ? "SERVICE_UNAVAILABLE"
          : "INTERNAL_ERROR");

      // If token refresh failed, the session carries a RefreshAccessTokenError.
      // The client-side SessionGuard / layout should detect this and call signOut().
      // We surface it here as a specific error code so callers can differentiate
      // an expired-and-unrefreshable token from a plain authorization failure.
      if (status === 401 && apiErr?.code === "AUTH_TOKEN_EXPIRED") {
        throw new ApiError(
          status,
          "AUTH_TOKEN_EXPIRED",
          message,
          undefined,
          apiErr?.detail ?? undefined,
          apiErr?.currentData ?? undefined,
          apiErr?.traceId
        );
      }

      // Flatten fields and convert PascalCase keys → camelCase to match form field names
      // e.g. { Name: ["msg1", "msg2"] } → { name: "msg1, msg2" }
      const fields: Record<string, string> | undefined =
        apiErr?.fields && Object.keys(apiErr.fields).length > 0
          ? Object.fromEntries(
              Object.entries(apiErr.fields).map(([k, v]) => [
                k.charAt(0).toLowerCase() + k.slice(1),
                Array.isArray(v) ? v.join(", ") : String(v),
              ])
            )
          : undefined;

      throw new ApiError(
        status,
        code,
        message,
        fields,
        apiErr?.detail ?? undefined,
        apiErr?.currentData ?? undefined,
        apiErr?.traceId
      );
    }
    throw error;
  }
);

export default client;
