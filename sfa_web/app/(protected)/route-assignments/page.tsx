'use client'

import dynamic from 'next/dynamic'

const DailyRouteAssignmentListPage = dynamic(
  () =>
    import('@/features/daily-route-assignment/components').then((m) => ({
      default: m.DailyRouteAssignmentListPage,
    })),
  { ssr: false },
)

export default function RouteAssignmentsPage() {
  return <DailyRouteAssignmentListPage />
}
