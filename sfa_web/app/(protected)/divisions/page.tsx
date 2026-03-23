'use client'

import dynamic from 'next/dynamic'
import { ErrorBoundary } from '@/components/error-boundary'
import { ErrorState } from '@/components/error-state'

const DivisionListPage = dynamic(
  () =>
    import('@/features/division/components').then((m) => ({
      default: m.DivisionListPage,
    })),
  { ssr: false },
)

export default function DivisionsPage() {
  return (
    <ErrorBoundary fallback={<ErrorState />}>
      <DivisionListPage />
    </ErrorBoundary>
  )
}
