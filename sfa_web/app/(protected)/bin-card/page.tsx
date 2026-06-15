"use client";

import dynamic from "next/dynamic";
import { ErrorBoundary } from "@/components/error-boundary";
import { ErrorState } from "@/components/error-state";

const BinCardPage = dynamic(
  () =>
    import("@/features/bin-card/components").then((m) => ({
      default: m.BinCardPage,
    })),
  { ssr: false },
);

export default function BinCardRoutePage() {
  return (
    <ErrorBoundary fallback={<ErrorState />}>
      <BinCardPage />
    </ErrorBoundary>
  );
}
