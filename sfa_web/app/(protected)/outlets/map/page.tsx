"use client";

import dynamic from "next/dynamic";
import { MapPageSkeleton } from "@/components/map-page-skeleton";

// Imported from its own file rather than the components barrel, and on demand, so neither the
// Google Maps library nor the outlet list/table code lands in this route's initial bundle.
const OutletMapPage = dynamic(
  () =>
    import("@/features/outlet/components/pages/outlet-map-page").then((m) => ({
      default: m.OutletMapPage,
    })),
  { ssr: false, loading: () => <MapPageSkeleton /> },
);

export default function OutletsMapPage() {
  return <OutletMapPage />;
}
