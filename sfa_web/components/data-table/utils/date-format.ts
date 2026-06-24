import { toColomboDateStr } from '@/lib/utils/datetime';

// Serializes a picked date to YYYY-MM-DD in Sri Lanka time so date-range filters are
// independent of the viewer's browser timezone. See lib/utils/datetime.ts.
export function formatDate(date: Date): string {
  return toColomboDateStr(date);
}