'use client'

import dynamic from 'next/dynamic'

const RouteListPage = dynamic(
  () =>
    import('@/features/route/components').then((m) => ({
      default: m.RouteListPage,
    })),
  { ssr: false },
)

export default function RoutesPage() {
  return <RouteListPage />
}
