"use client";

import dynamic from "next/dynamic";
import { ErrorBoundary } from "@/components/error-boundary";
import { ErrorState } from "@/components/error-state";

const UserListPage = dynamic(
  () =>
    import("@/features/user/components").then((m) => ({
      default: m.UserListPage,
    })),
  { ssr: false },
);

export default function UsersPage() {
  return (
    <ErrorBoundary fallback={<ErrorState />}>
      <UserListPage />
    </ErrorBoundary>
  );
}
