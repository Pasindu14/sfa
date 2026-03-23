"use client";

import dynamic from "next/dynamic";
import { ErrorBoundary } from "@/components/error-boundary";
import { ErrorState } from "@/components/error-state";

const TerritoryListPage = dynamic(
  () =>
    import("@/features/territory/components").then((m) => ({
      default: m.TerritoryListPage,
    })),
  { ssr: false },
);

export default function TerritoriesPage() {
  return (
    <ErrorBoundary fallback={<ErrorState />}>
      <TerritoryListPage />
    </ErrorBoundary>
  );
}
