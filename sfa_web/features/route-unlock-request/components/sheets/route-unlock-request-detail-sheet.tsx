'use client'

import { Ban, CheckCircle, CheckCircle2, Circle, Clock, ExternalLink, XCircle } from 'lucide-react'
import {
  Sheet,
  SheetContent,
  SheetDescription,
  SheetHeader,
  SheetTitle,
} from '@/components/ui/sheet'
import { Button } from '@/components/ui/button'
import { Skeleton } from '@/components/ui/skeleton'
import { Separator } from '@/components/ui/separator'
import { RouteUnlockStatusBadge } from '../badges/route-unlock-status-badge'
import {
  useApproveUnlockDialog,
  useRejectUnlockDialog,
  useRevokeUnlockDialog,
  useUnlockDetailSheet,
} from '../../store'
import { useRouteUnlockRequest } from '../../hooks/route-unlock-request.hooks'
import {
  RouteUnlockStatus,
  type RouteUnlockBillDto,
  type RouteUnlockEventAction,
  type RouteUnlockRequestDto,
  type RouteUnlockRequestEventDto,
} from '../../schema/route-unlock-request.schema'
import { formatBusinessDate, formatLkr, formatTimestamp } from '../format'

// ── Event timeline (same shape as the PO HistoryTimeline) ─────────────────

const eventActionConfig: Record<RouteUnlockEventAction, { color: string; label: string }> = {
  Requested: { color: 'bg-blue-500', label: 'Requested' },
  Approved: { color: 'bg-green-500', label: 'Approved' },
  Rejected: { color: 'bg-red-500', label: 'Rejected' },
  Cancelled: { color: 'bg-slate-400', label: 'Cancelled' },
  Revoked: { color: 'bg-orange-500', label: 'Revoked' },
}

function EventTimeline({ events }: { events: RouteUnlockRequestEventDto[] }) {
  if (events.length === 0) {
    return <p className="text-sm text-muted-foreground">No events recorded.</p>
  }

  return (
    <div className="space-y-0">
      {events.map((entry, idx) => {
        const cfg = eventActionConfig[entry.action] ?? { color: 'bg-gray-300', label: entry.action }
        const isLast = idx === events.length - 1

        return (
          <div key={entry.id} className="flex gap-3">
            <div className="flex flex-col items-center">
              <div
                className={`w-3 h-3 rounded-full mt-1 shrink-0 ${cfg.color} ${isLast ? 'ring-2 ring-offset-1 ring-orange-400' : ''}`}
              />
              {!isLast && <div className="w-px flex-1 bg-border mt-1" />}
            </div>
            <div className="pb-4 flex-1 min-w-0">
              <div className="flex items-center gap-2 flex-wrap">
                <p className="text-sm font-medium leading-tight">
                  {cfg.label} — {entry.performedByName ?? 'Unknown'}
                  <span className="text-xs font-normal text-muted-foreground">
                    {' '}
                    · {entry.performedByRole}
                  </span>
                </p>
                {isLast && (
                  <span className="text-[10px] font-semibold uppercase bg-orange-100 text-orange-700 px-1.5 py-0.5 rounded">
                    CURRENT
                  </span>
                )}
              </div>
              <p className="text-xs text-muted-foreground mt-0.5">
                {formatTimestamp(entry.performedAt)}
                {entry.ipAddress && <span> · {entry.ipAddress}</span>}
              </p>
              {entry.note && (
                <p className="text-xs text-muted-foreground mt-1 italic bg-muted/50 px-2 py-1 rounded">
                  {entry.action === 'Approved' ? 'Note' : 'Reason'}: {entry.note}
                </p>
              )}
            </div>
          </div>
        )
      })}
    </div>
  )
}

// ── Facts ──────────────────────────────────────────────────────────────────

function Fact({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <div className="space-y-0.5">
      <p className="text-xs font-medium text-muted-foreground uppercase tracking-wide">{label}</p>
      <div className="text-sm">{children}</div>
    </div>
  )
}

function RequestFacts({ request }: { request: RouteUnlockRequestDto }) {
  const hasGps = request.requestLatitude !== null && request.requestLongitude !== null

  return (
    <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
      <Fact label="Rep">
        <span className="font-medium">{request.userName}</span>
        <span className="text-muted-foreground"> ({request.loginName})</span>
      </Fact>
      <Fact label="Route">{request.routeName}</Fact>
      <Fact label="Business date">{formatBusinessDate(request.businessDate)}</Fact>
      <Fact label="Requested at">{formatTimestamp(request.requestedAt)}</Fact>
      <Fact label="Routed to">
        {request.supervisorName ?? (
          <span className="text-muted-foreground">— (no supervisor, admin only)</span>
        )}
      </Fact>
      <Fact label="Request location">
        {hasGps ? (
          <span className="inline-flex items-center gap-1.5 flex-wrap">
            <a
              href={`https://www.google.com/maps?q=${request.requestLatitude},${request.requestLongitude}`}
              target="_blank"
              rel="noopener noreferrer"
              className="inline-flex items-center gap-1 text-primary underline underline-offset-2"
            >
              {request.requestLatitude!.toFixed(5)}, {request.requestLongitude!.toFixed(5)}
              <ExternalLink className="h-3 w-3" />
            </a>
            {request.requestGpsAccuracyMeters !== null && (
              <span className="text-xs text-muted-foreground">
                ±{Math.round(request.requestGpsAccuracyMeters)} m
              </span>
            )}
          </span>
        ) : (
          <span className="text-muted-foreground">Not captured</span>
        )}
      </Fact>
      <div className="sm:col-span-2">
        <Fact label="Reason">
          <p className="whitespace-pre-wrap">{request.requestReason}</p>
        </Fact>
      </div>

      {request.reviewedByName && (
        <>
          <Fact label="Reviewed by">
            {request.reviewedByName}
            {request.reviewedByRole && (
              <span className="text-muted-foreground"> · {request.reviewedByRole}</span>
            )}
            <div className="text-xs text-muted-foreground">{formatTimestamp(request.reviewedAt)}</div>
          </Fact>
          <Fact label={request.status === RouteUnlockStatus.Rejected ? 'Rejection reason' : 'Review note'}>
            {request.reviewNote ?? <span className="text-muted-foreground">—</span>}
          </Fact>
        </>
      )}

      {request.validFrom && (
        <div className="sm:col-span-2">
          <Fact label="Valid window">
            {formatTimestamp(request.validFrom)} → {formatTimestamp(request.validTo)}
          </Fact>
        </div>
      )}

      {request.revokedAt && (
        <>
          <Fact label="Revoked by">
            {request.revokedByName ?? '—'}
            <div className="text-xs text-muted-foreground">{formatTimestamp(request.revokedAt)}</div>
          </Fact>
          <Fact label="Revoke reason">{request.revokeReason ?? '—'}</Fact>
        </>
      )}

      {request.cancelledAt && (
        <Fact label="Cancelled by rep">{formatTimestamp(request.cancelledAt)}</Fact>
      )}
    </div>
  )
}

// ── Bills ──────────────────────────────────────────────────────────────────

function BillsTable({ bills }: { bills: RouteUnlockBillDto[] }) {
  if (bills.length === 0) {
    return <p className="text-sm text-muted-foreground">No bills were placed using this unlock.</p>
  }

  return (
    <div className="rounded-md border overflow-x-auto">
      <table className="w-full text-sm">
        <thead className="bg-muted/50 text-xs text-muted-foreground">
          <tr>
            <th className="text-left font-medium px-3 py-2">Bill #</th>
            <th className="text-left font-medium px-3 py-2">Date</th>
            <th className="text-left font-medium px-3 py-2">Outlet</th>
            <th className="text-right font-medium px-3 py-2">Distance</th>
            <th className="text-right font-medium px-3 py-2">Total</th>
          </tr>
        </thead>
        <tbody>
          {bills.map((bill) => (
            <tr key={bill.billingId} className="border-t">
              <td className="px-3 py-2 font-mono">{bill.billingNumber}</td>
              <td className="px-3 py-2 whitespace-nowrap">{formatTimestamp(bill.billingDate)}</td>
              <td className="px-3 py-2">{bill.outletName}</td>
              <td className="px-3 py-2 text-right whitespace-nowrap">
                {bill.distanceFromOutletMeters !== null
                  ? `${Math.round(bill.distanceFromOutletMeters).toLocaleString()} m`
                  : '—'}
              </td>
              <td className="px-3 py-2 text-right whitespace-nowrap">{formatLkr(bill.totalAmount)}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}

// ── Sheet ──────────────────────────────────────────────────────────────────

function SectionTitle({ children }: { children: React.ReactNode }) {
  return <h3 className="text-sm font-semibold mb-3">{children}</h3>
}

export function RouteUnlockRequestDetailSheet() {
  const { isOpen, detailId, close } = useUnlockDetailSheet()
  const { data, isLoading, isError } = useRouteUnlockRequest(detailId)
  const approveDialog = useApproveUnlockDialog()
  const rejectDialog = useRejectUnlockDialog()
  const revokeDialog = useRevokeUnlockDialog()

  const request = data?.request
  const selected = request
    ? {
        id: request.id,
        rowVersion: request.rowVersion,
        repName: request.userName,
        routeName: request.routeName,
      }
    : null
  const isPending = request?.effectiveStatus === RouteUnlockStatus.Pending

  return (
    <Sheet open={isOpen} onOpenChange={(open) => !open && close()}>
      <SheetContent
        side="right"
        className="w-full data-[side=right]:sm:max-w-2xl overflow-y-auto"
      >
        <SheetHeader>
          <div className="flex items-center gap-2 pr-8">
            <SheetTitle>Unlock Request {request ? `#${request.id}` : ''}</SheetTitle>
            {request && <RouteUnlockStatusBadge status={request.effectiveStatus} />}
          </div>
          <SheetDescription>
            A rep&apos;s request to reach every outlet on today&apos;s route, and everything that
            happened to it.
          </SheetDescription>
        </SheetHeader>

        <div className="px-4 pb-6 space-y-6">
          {isLoading && (
            <div className="space-y-3">
              <Skeleton className="h-5 w-1/2" />
              <Skeleton className="h-24 w-full" />
              <Skeleton className="h-32 w-full" />
            </div>
          )}

          {isError && (
            <p className="text-sm text-destructive">Could not load this unlock request.</p>
          )}

          {request && data && (
            <>
              {(isPending || request.isCurrentlyEffective) && selected && (
                <div className="flex flex-wrap gap-2">
                  {isPending && (
                    <>
                      <Button
                        size="sm"
                        className="gap-1.5 bg-green-600 hover:bg-green-700"
                        onClick={() => approveDialog.open(selected)}
                      >
                        <CheckCircle2 className="h-4 w-4" />
                        Approve
                      </Button>
                      <Button
                        size="sm"
                        variant="destructive"
                        className="gap-1.5"
                        onClick={() => rejectDialog.open(selected)}
                      >
                        <XCircle className="h-4 w-4" />
                        Reject
                      </Button>
                    </>
                  )}
                  {request.isCurrentlyEffective && (
                    <Button
                      size="sm"
                      variant="outline"
                      className="gap-1.5 text-orange-700 border-orange-200 hover:bg-orange-50"
                      onClick={() => revokeDialog.open(selected)}
                    >
                      <Ban className="h-4 w-4" />
                      Revoke
                    </Button>
                  )}
                </div>
              )}

              <section>
                <SectionTitle>Request</SectionTitle>
                <RequestFacts request={request} />
              </section>

              <Separator />

              <section>
                <SectionTitle>
                  <span className="inline-flex items-center gap-1.5">
                    <Clock className="h-4 w-4 text-muted-foreground" />
                    Timeline
                  </span>
                </SectionTitle>
                <EventTimeline events={data.events} />
              </section>

              <Separator />

              <section>
                <SectionTitle>
                  <span className="inline-flex items-center gap-1.5">
                    {data.bills.length > 0 ? (
                      <CheckCircle className="h-4 w-4 text-muted-foreground" />
                    ) : (
                      <Circle className="h-4 w-4 text-muted-foreground" />
                    )}
                    Bills placed using this unlock ({data.bills.length})
                  </span>
                </SectionTitle>
                <BillsTable bills={data.bills} />
              </section>
            </>
          )}
        </div>
      </SheetContent>
    </Sheet>
  )
}
