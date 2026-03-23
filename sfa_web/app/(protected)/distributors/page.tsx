"use client";

import dynamic from "next/dynamic";
import { ErrorBoundary } from "@/components/error-boundary";
import { ErrorState } from "@/components/error-state";

const DistributorListPage = dynamic(
  () =>
    import("@/features/distributor/components").then((m) => ({
      default: m.DistributorListPage,
    })),
  { ssr: false },
);

export default function DistributorsPage() {
  return (
    <ErrorBoundary fallback={<ErrorState />}>
      <DistributorListPage />
    </ErrorBoundary>
  );
}
