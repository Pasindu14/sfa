"use client";

import { use } from "react";
import dynamic from "next/dynamic";
import { ErrorBoundary } from "@/components/error-boundary";
import { ErrorState } from "@/components/error-state";

const StockTakingReviewPage = dynamic(
  () =>
    import("@/features/stock-taking/components").then((m) => ({
      default: m.StockTakingReviewPage,
    })),
  { ssr: false },
);

export default function StockTakingReviewRoute({
  params,
}: {
  params: Promise<{ periodId: string }>;
}) {
  const { periodId } = use(params);
  return (
    <ErrorBoundary fallback={<ErrorState />}>
      <StockTakingReviewPage periodId={Number(periodId)} />
    </ErrorBoundary>
  );
}
