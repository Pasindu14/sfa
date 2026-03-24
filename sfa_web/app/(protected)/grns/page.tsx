"use client";

import dynamic from "next/dynamic";
import { ErrorBoundary } from "@/components/error-boundary";
import { ErrorState } from "@/components/error-state";

const GrnPage = dynamic(
  () =>
    import("@/features/grn/components").then((m) => ({
      default: m.GrnPage,
    })),
  { ssr: false },
);

export default function GrnsPage() {
  return (
    <ErrorBoundary fallback={<ErrorState />}>
      <GrnPage />
    </ErrorBoundary>
  );
}
