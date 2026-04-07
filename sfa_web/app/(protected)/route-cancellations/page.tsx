'use client'

import dynamic from 'next/dynamic'
import { ErrorBoundary } from '@/components/error-boundary'
import { ErrorState } from '@/components/error-state'

const RouteCancellationListPage = dynamic(
  () =>
    import(
      '@/features/route-cancellation/components/pages/route-cancellation-list-page'
    ).then((m) => ({ default: m.RouteCancellationListPage })),
  { ssr: false }
)

export default function RouteCancellationsPage() {
  return (
    <ErrorBoundary fallback={<ErrorState />}>
      <RouteCancellationListPage />
    </ErrorBoundary>
  )
}
