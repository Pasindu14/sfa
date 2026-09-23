"use client";

import dynamic from "next/dynamic";
import { ErrorBoundary } from "@/components/error-boundary";
import { ErrorState } from "@/components/error-state";

const StockAdjustmentPage = dynamic(
  () =>
    import("@/features/stock-adjustment/components").then((m) => ({
      default: m.StockAdjustmentPage,
    })),
  { ssr: false },
);

export default function StockAdjustmentRoutePage() {
  return (
    <ErrorBoundary fallback={<ErrorState />}>
      <StockAdjustmentPage />
    </ErrorBoundary>
  );
}
