"use client";

import dynamic from "next/dynamic";
import { ErrorBoundary } from "@/components/error-boundary";
import { ErrorState } from "@/components/error-state";

const SalesTargetListPage = dynamic(
  () =>
    import("@/features/sales-target/components/pages/sales-target-list-page").then((m) => ({
      default: m.SalesTargetListPage,
    })),
  { ssr: false },
);

export default function SalesTargetsPage() {
  return (
    <ErrorBoundary fallback={<ErrorState />}>
      <SalesTargetListPage />
    </ErrorBoundary>
  );
}
