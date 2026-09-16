'use client'

import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Separator } from '@/components/ui/separator'
import { Spinner } from '@/components/ui/spinner'
import { formatColombo } from '@/lib/utils/datetime'
import {
  useCurrentExemption,
  useExemptionHistory,
  useGrantExemption,
  useRevokeExemption,
} from '../../hooks/proximity-exemption.hooks'
import { exemptionReasonLabels } from '../../schema/proximity-exemption.schema'
import type { ProximityExemptionDto } from '../../schema/proximity-exemption.schema'
import { GrantExemptionForm } from '../forms/grant-exemption-form'

interface LocationPolicyDialogProps {
  isOpen: boolean
  userId: number | null
  userName?: string
  onClose: () => void
}

/**
 * Admin-only control for one rep's billing geofence.
 *
 * A separate dialog rather than a section on the user edit form: an exemption is
 * its own resource with its own lifecycle (grant, revoke, expire) and its own
 * concurrency token, so folding it into the user form would mean two unrelated
 * things saving under one button.
 */
export function LocationPolicyDialog({
  isOpen,
  userId,
  userName,
  onClose,
}: LocationPolicyDialogProps) {
  const current = useCurrentExemption(isOpen ? userId : null)
  const history = useExemptionHistory(isOpen ? userId : null)
  const grant = useGrantExemption(userId)
  const revoke = useRevokeExemption(userId)

  const live = current.data ?? null

  return (
    <Dialog
      open={isOpen}
      onOpenChange={(open) => {
        if (!open) {
          onClose()
          grant.clearFieldErrors()
        }
      }}
    >
      <DialogContent className="max-h-[85vh] max-w-lg overflow-y-auto">
        <DialogHeader>
          <DialogTitle>Location policy</DialogTitle>
          <DialogDescription>
            {userName
              ? `Billing geofence for ${userName}.`
              : 'Billing geofence for this rep.'}{' '}
            Reps normally may only bill outlets within the configured radius.
          </DialogDescription>
        </DialogHeader>

        {current.isLoading ? (
          <div className="flex justify-center py-8">
            <Spinner />
          </div>
        ) : (
          <div className="space-y-4">
            {live ? (
              <ActiveExemptionCard
                exemption={live}
                isRevoking={revoke.isPending}
                onRevoke={() =>
                  revoke.mutate({
                    exemptionId: live.id,
                    rowVersion: live.rowVersion,
                  })
                }
              />
            ) : (
              <div className="rounded-md border p-3 text-sm">
                <div className="flex items-center gap-2">
                  <Badge variant="default">Geofence enforced</Badge>
                </div>
                <p className="text-muted-foreground mt-2">
                  This rep can only bill outlets they are standing near. Grant an
                  exemption below to lift that temporarily.
                </p>
              </div>
            )}

            <Separator />

            <div>
              <h4 className="mb-3 text-sm font-medium">
                {live ? 'Replace this exemption' : 'Grant an exemption'}
              </h4>
              <GrantExemptionForm
                // The rep is already known here, so the picker stays off and its
                // placeholder userId is dropped rather than sent in the body.
                onSubmit={({ userId: _userId, ...data }) => grant.mutate(data)}
                isLoading={grant.isPending}
                fieldErrors={grant.fieldErrors}
              />
              {live && (
                <p className="text-muted-foreground mt-2 text-xs">
                  Granting a new exemption supersedes the one above; both stay in
                  the history.
                </p>
              )}
            </div>

            {history.data && history.data.length > 0 && (
              <>
                <Separator />
                <HistoryList items={history.data} />
              </>
            )}
          </div>
        )}
      </DialogContent>
    </Dialog>
  )
}

function ActiveExemptionCard({
  exemption,
  isRevoking,
  onRevoke,
}: {
  exemption: ProximityExemptionDto
  isRevoking: boolean
  onRevoke: () => void
}) {
  return (
    <div className="space-y-3 rounded-md border border-amber-500/40 bg-amber-500/5 p-3 text-sm">
      <div className="flex items-center justify-between gap-2">
        <Badge variant="secondary">Geofence lifted</Badge>
        <Button
          variant="outline"
          size="sm"
          onClick={onRevoke}
          disabled={isRevoking}
        >
          {isRevoking && <Spinner className="mr-2 size-3" />}
          Revoke now
        </Button>
      </div>

      <dl className="grid grid-cols-[auto_1fr] gap-x-3 gap-y-1">
        <dt className="text-muted-foreground">Applies through</dt>
        <dd>{formatColombo(exemption.validUntilDate)}</dd>
        <dt className="text-muted-foreground">Reason</dt>
        <dd>{exemptionReasonLabels[exemption.reason] ?? exemption.reason}</dd>
        <dt className="text-muted-foreground">Granted by</dt>
        <dd>{exemption.grantedByUserName ?? '—'}</dd>
        {exemption.notes && (
          <>
            <dt className="text-muted-foreground">Notes</dt>
            <dd className="whitespace-pre-wrap">{exemption.notes}</dd>
          </>
        )}
      </dl>

      <p className="text-muted-foreground text-xs">
        Revoking takes effect immediately — the server re-checks every bill. The
        rep&apos;s app catches up on its next sync.
      </p>
    </div>
  )
}

function HistoryList({ items }: { items: ProximityExemptionDto[] }) {
  return (
    <div>
      <h4 className="mb-2 text-sm font-medium">History</h4>
      <ul className="space-y-2 text-xs">
        {items.map((e) => (
          <li
            key={e.id}
            className="flex items-start justify-between gap-3 rounded border p-2"
          >
            <div>
              <div className="font-medium">
                {exemptionReasonLabels[e.reason] ?? e.reason}
              </div>
              <div className="text-muted-foreground">
                {formatColombo(e.validFrom)} – {formatColombo(e.validUntilDate)}
                {e.grantedByUserName ? ` · by ${e.grantedByUserName}` : ''}
              </div>
              {e.notes && (
                <div className="text-muted-foreground mt-1 whitespace-pre-wrap">
                  {e.notes}
                </div>
              )}
            </div>
            <Badge variant={e.isCurrentlyEffective ? 'secondary' : 'outline'}>
              {e.isCurrentlyEffective
                ? 'Active'
                : e.revokedAt
                  ? 'Revoked'
                  : 'Expired'}
            </Badge>
          </li>
        ))}
      </ul>
    </div>
  )
}
