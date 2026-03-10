"use client";
import dynamic from "next/dynamic";

const TerritoryListPage = dynamic(
  () =>
    import("@/features/territory/components").then((m) => ({
      default: m.TerritoryListPage,
    })),
  { ssr: false },
);

export default function TerritoriesPage() {
  return <TerritoryListPage />;
}
