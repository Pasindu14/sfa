'use client'

import dynamic from 'next/dynamic'

const DivisionListPage = dynamic(
  () =>
    import('@/features/division/components').then((m) => ({
      default: m.DivisionListPage,
    })),
  { ssr: false },
)

export default function DivisionsPage() {
  return <DivisionListPage />
}
