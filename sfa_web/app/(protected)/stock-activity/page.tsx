"use client";

import dynamic from "next/dynamic";
import { ErrorBoundary } from "@/components/error-boundary";
import { ErrorState } from "@/components/error-state";

const StockActivityPage = dynamic(
  () =>
    import("@/features/stock-activity/components").then((m) => ({
      default: m.StockActivityPage,
    })),
  { ssr: false },
);

export default function StockActivityRoutePage() {
  return (
    <ErrorBoundary fallback={<ErrorState />}>
      <StockActivityPage />
    </ErrorBoundary>
  );
}
