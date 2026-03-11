"use client";
import dynamic from "next/dynamic";

const OutletListPage = dynamic(
  () =>
    import("@/features/outlet/components").then((m) => ({
      default: m.OutletListPage,
    })),
  { ssr: false },
);

export default function OutletsPage() {
  return <OutletListPage />;
}
