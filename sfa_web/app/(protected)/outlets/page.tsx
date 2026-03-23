"use client";

import dynamic from "next/dynamic";
import { ErrorBoundary } from "@/components/error-boundary";
import { ErrorState } from "@/components/error-state";

const OutletListPage = dynamic(
  () =>
    import("@/features/outlet/components").then((m) => ({
      default: m.OutletListPage,
    })),
  { ssr: false },
);

export default function OutletsPage() {
  return (
    <ErrorBoundary fallback={<ErrorState />}>
      <OutletListPage />
    </ErrorBoundary>
  );
}
