"use client";
import dynamic from "next/dynamic";

const DistributorListPage = dynamic(
  () =>
    import("@/features/distributor/components").then((m) => ({
      default: m.DistributorListPage,
    })),
  { ssr: false },
);

export default function DistributorsPage() {
  return <DistributorListPage />;
}
