"use client";

import dynamic from "next/dynamic";
import { ErrorBoundary } from "@/components/error-boundary";
import { ErrorState } from "@/components/error-state";

const StockPage = dynamic(
  () =>
    import("@/features/stock/components").then((m) => ({
      default: m.StockPage,
    })),
  { ssr: false },
);

export default function StockDashboardPage() {
  return (
    <ErrorBoundary fallback={<ErrorState />}>
      <StockPage />
    </ErrorBoundary>
  );
}
