"use client";

import dynamic from "next/dynamic";
import { ErrorBoundary } from "@/components/error-boundary";
import { ErrorState } from "@/components/error-state";

const AreaListPage = dynamic(
  () =>
    import("@/features/area/components").then((m) => ({
      default: m.AreaListPage,
    })),
  { ssr: false },
);

export default function AreasPage() {
  return (
    <ErrorBoundary fallback={<ErrorState />}>
      <AreaListPage />
    </ErrorBoundary>
  );
}
