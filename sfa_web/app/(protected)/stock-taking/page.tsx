"use client";

import dynamic from "next/dynamic";
import { ErrorBoundary } from "@/components/error-boundary";
import { ErrorState } from "@/components/error-state";

const StockTakingListPage = dynamic(
  () =>
    import("@/features/stock-taking/components").then((m) => ({
      default: m.StockTakingListPage,
    })),
  { ssr: false },
);

export default function StockTakingPage() {
  return (
    <ErrorBoundary fallback={<ErrorState />}>
      <StockTakingListPage />
    </ErrorBoundary>
  );
}
