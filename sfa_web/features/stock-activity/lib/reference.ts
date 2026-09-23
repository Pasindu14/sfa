import type { StockActivity } from '../schema/stock-activity.schema'

/** Human reference for an activity row — the document number when known, else type + id. */
export function formatReference(row: Pick<StockActivity, 'referenceNumber' | 'referenceType' | 'referenceId'>): string {
  if (row.referenceNumber) return row.referenceNumber
  if (row.referenceType && row.referenceId) return `${row.referenceType} #${row.referenceId}`
  return row.referenceType ?? ''
}
