"use client";

import dynamic from "next/dynamic";
import { ErrorBoundary } from "@/components/error-boundary";
import { ErrorState } from "@/components/error-state";

const RegionListPage = dynamic(
  () =>
    import("@/features/region/components").then((m) => ({
      default: m.RegionListPage,
    })),
  { ssr: false },
);

export default function RegionsPage() {
  return (
    <ErrorBoundary fallback={<ErrorState />}>
      <RegionListPage />
    </ErrorBoundary>
  );
}
