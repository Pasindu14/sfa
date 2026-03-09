"use client";
import dynamic from "next/dynamic";

const RegionListPage = dynamic(
  () =>
    import("@/features/region/components").then((m) => ({
      default: m.RegionListPage,
    })),
  { ssr: false },
);

export default function RegionsPage() {
  return <RegionListPage />;
}
