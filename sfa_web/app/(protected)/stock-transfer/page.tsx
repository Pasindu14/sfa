"use client";

import dynamic from "next/dynamic";
import { ErrorBoundary } from "@/components/error-boundary";
import { ErrorState } from "@/components/error-state";

const StockTransferPage = dynamic(
  () =>
    import("@/features/stock-transfer/components").then((m) => ({
      default: m.StockTransferPage,
    })),
  { ssr: false },
);

export default function StockTransferRoutePage() {
  return (
    <ErrorBoundary fallback={<ErrorState />}>
      <StockTransferPage />
    </ErrorBoundary>
  );
}
