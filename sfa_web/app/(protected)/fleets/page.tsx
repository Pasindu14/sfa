"use client";

import dynamic from "next/dynamic";
import { ErrorBoundary } from "@/components/error-boundary";
import { ErrorState } from "@/components/error-state";

const FleetListPage = dynamic(
  () =>
    import("@/features/fleet/components").then((m) => ({
      default: m.FleetListPage,
    })),
  { ssr: false },
);

export default function FleetsPage() {
  return (
    <ErrorBoundary fallback={<ErrorState />}>
      <FleetListPage />
    </ErrorBoundary>
  );
}
