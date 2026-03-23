'use client'

import dynamic from 'next/dynamic'
import { ErrorBoundary } from '@/components/error-boundary'
import { ErrorState } from '@/components/error-state'

const RouteListPage = dynamic(
  () =>
    import('@/features/route/components').then((m) => ({
      default: m.RouteListPage,
    })),
  { ssr: false },
)

export default function RoutesPage() {
  return (
    <ErrorBoundary fallback={<ErrorState />}>
      <RouteListPage />
    </ErrorBoundary>
  )
}
