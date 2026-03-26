'use client'

import dynamic from 'next/dynamic'

const UserGeoAssignmentListPage = dynamic(
  () =>
    import('@/features/user-geo-assignment/components').then((m) => ({
      default: m.UserGeoAssignmentListPage,
    })),
  { ssr: false },
)

export default function GeoAssignmentsPage() {
  return <UserGeoAssignmentListPage />
}
