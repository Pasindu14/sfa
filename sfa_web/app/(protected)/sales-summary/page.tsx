"use client";

import dynamic from "next/dynamic";
import { ErrorBoundary } from "@/components/error-boundary";
import { ErrorState } from "@/components/error-state";

const SalesSummaryPage = dynamic(
  () =>
    import("@/features/sales-summary/components").then((m) => ({
      default: m.SalesSummaryPage,
    })),
  { ssr: false },
);

export default function SalesSummaryRoutePage() {
  return (
    <ErrorBoundary fallback={<ErrorState />}>
      <SalesSummaryPage />
    </ErrorBoundary>
  );
}
