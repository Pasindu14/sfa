'use client'

import dynamic from 'next/dynamic'
import { ErrorBoundary } from '@/components/error-boundary'
import { ErrorState } from '@/components/error-state'

const RouteUnlockRequestListPage = dynamic(
  () =>
    import(
      '@/features/route-unlock-request/components/pages/route-unlock-request-list-page'
    ).then((m) => ({ default: m.RouteUnlockRequestListPage })),
  { ssr: false }
)

export default function RouteUnlockRequestsPage() {
  return (
    <ErrorBoundary fallback={<ErrorState />}>
      <RouteUnlockRequestListPage />
    </ErrorBoundary>
  )
}
