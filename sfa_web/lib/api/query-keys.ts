/**
 * Query keys shared across features.
 *
 * Several features cache the full user list under their own key to back
 * AsyncSelect dropdowns (reporting lines, geo assignments). Those caches live
 * outside `userKeys.all`, so any mutation that changes a user's status must
 * invalidate them explicitly — otherwise a deactivated user keeps showing up in
 * the dropdowns until the stale time expires.
 */
export const userSelectKeys = {
  reportingLine: ['users-for-select'] as const,
  geoAssignment: ['geo-users-for-select'] as const,
}

/** Every cached "all users" dataset — invalidate all of these after a user mutation. */
export const allUserSelectKeys = [
  userSelectKeys.reportingLine,
  userSelectKeys.geoAssignment,
] as const
