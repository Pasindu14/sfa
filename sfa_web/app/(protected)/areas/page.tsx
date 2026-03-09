"use client";
import dynamic from "next/dynamic";

const AreaListPage = dynamic(
  () =>
    import("@/features/area/components").then((m) => ({
      default: m.AreaListPage,
    })),
  { ssr: false },
);

export default function AreasPage() {
  return <AreaListPage />;
}
