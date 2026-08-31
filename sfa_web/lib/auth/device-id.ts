const DEVICE_ID_STORAGE_KEY = "sfa_web_device_id"

/**
 * Stable, per-browser identifier for TOFU device binding on the API
 * (see AuthService.LoginAsync). Generated once and persisted in
 * localStorage; survives reloads and re-logins but not clearing site data.
 */
export function getOrCreateDeviceId(): string {
  const existing = window.localStorage.getItem(DEVICE_ID_STORAGE_KEY)
  if (existing) return existing

  const id = crypto.randomUUID()
  window.localStorage.setItem(DEVICE_ID_STORAGE_KEY, id)
  return id
}
