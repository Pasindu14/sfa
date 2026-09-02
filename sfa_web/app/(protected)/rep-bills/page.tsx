"use client";

import dynamic from "next/dynamic";
import { ErrorBoundary } from "@/components/error-boundary";
import { ErrorState } from "@/components/error-state";

const RepBillPage = dynamic(
  () =>
    import("@/features/rep-bill/components").then((m) => ({
      default: m.RepBillPage,
    })),
  { ssr: false },
);

export default function RepBillsPage() {
  return (
    <ErrorBoundary fallback={<ErrorState />}>
      <RepBillPage />
    </ErrorBoundary>
  );
}
