"use client";

import dynamic from "next/dynamic";
import { ErrorBoundary } from "@/components/error-boundary";
import { ErrorState } from "@/components/error-state";

const DistributorStockTakingPage = dynamic(
  () =>
    import("@/features/distributor-stock-taking/components").then((m) => ({
      default: m.DistributorStockTakingPage,
    })),
  { ssr: false },
);

export default function DistributorStockTakingRoute() {
  return (
    <ErrorBoundary fallback={<ErrorState />}>
      <DistributorStockTakingPage />
    </ErrorBoundary>
  );
}
