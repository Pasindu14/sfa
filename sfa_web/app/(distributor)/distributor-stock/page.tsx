"use client";

import dynamic from "next/dynamic";
import { ErrorBoundary } from "@/components/error-boundary";
import { ErrorState } from "@/components/error-state";

const DistributorStockPage = dynamic(
  () =>
    import("@/features/distributor-stock/components").then((m) => ({
      default: m.DistributorStockPage,
    })),
  { ssr: false },
);

export default function DistributorStockRoute() {
  return (
    <ErrorBoundary fallback={<ErrorState />}>
      <DistributorStockPage />
    </ErrorBoundary>
  );
}
