'use client'

import dynamic from 'next/dynamic'

const UserReportingLineListPage = dynamic(
  () =>
    import('@/features/user-reporting-line/components').then((m) => ({
      default: m.UserReportingLineListPage,
    })),
  { ssr: false },
)

export default function UserReportingLinesPage() {
  return <UserReportingLineListPage />
}
