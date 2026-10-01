# Duplicate-protection fixes (from the 2026-10-01 audit)

## API contract changes (built by the API owner; clients code against this)
1. `PATCH /api/v1/billings/{id}/cancel` — a bill the distributor already REJECTED can now be cancelled by the rep
   (RepStatus → Cancelled) but its stock is NOT reversed a second time. Response shape unchanged.
2. NEW `GET /api/v1/billings/by-client-id/{clientBillId}` (SalesRep, own bills only) →
   `200 ApiResponse<BillingDto>` when the server holds a bill with that client bill id (BillingDto has `id`,
   `repStatus`, `distributorStatus`, `billingNumber`), `404 NOT_FOUND` when it does not. Used by the phone
   before it deletes a bill that never got a confirmed sync.
3. `POST /api/v1/stock-transfers` — the `X-Idempotency-Key` header is now persisted on the transfer under a
   unique index; a replay with the same key returns the SAME transfer (201 body of the original) and moves
   no stock. Missing header still works (no dedupe), the web always sends one.

## Mobile (sfa_mobile)
- Stuck `syncing` rows (bills + not_billings): reset to `pending` on app start and at the top of every flush,
  unless the row is genuinely in flight in THIS isolate or was claimed very recently by another (use a stale
  threshold, e.g. last_attempt_at older than 2 min). Re-send is safe (stable client id).
- Atomic claim: `UPDATE … SET sync_status='syncing' WHERE id=? AND sync_status IN ('pending','failed')`, proceed
  only if 1 row changed. Never downgrade a `synced` or `cancelled` row in markFailed / markPendingAfterNetworkError.
- Delete of a bill/visit that is pending/failed: if it has no serverBillId and any attempt was made
  (sync_attempts > 0 or status was syncing), call the new lookup. Found → cancel server-side with that id, then mark
  cancelled locally. 404 → cancel locally. Network error → refuse with "Connect to the internet to delete this bill".
  Refuse delete while the row is in flight. Fix the dialog copy that says the server won't be affected.
- Handler-level double-submit guards (`droppable()` / already-submitted check) in create-bill and create-outlet blocs.

## Web (sfa_web)
- Staff + distributor PO create pages: one latch covering create AND the chained submit (button disabled until
  navigation); if create succeeds but submit fails, remember the created PO id and retry ONLY the submit (or
  navigate to the draft with a toast) — never create a second PO.
- Stock transfer: keep the caller-owned idempotency key for the same payload across ambiguous failures
  (network/timeout/5xx); mint a new key only after a definitive business/validation rejection or a changed payload.
- Fix stale comment at lib/api/client.ts:74-78.
