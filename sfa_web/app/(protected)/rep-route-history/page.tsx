"use client";

import dynamic from "next/dynamic";
import { MapPageSkeleton } from "@/components/map-page-skeleton";

// The page (and its APIProvider) is loaded on demand so the Google Maps library stays out of
// the route's initial bundle.
const RepRoutePage = dynamic(
  () =>
    import("@/features/rep-route/components/pages/rep-route-page").then((m) => ({
      default: m.RepRoutePage,
    })),
  {
    ssr: false,
    loading: () => <MapPageSkeleton mapHeight="calc(100vh - 320px)" withFilterBar />,
  },
);

export default function RepRouteHistoryPage() {
  return <RepRoutePage />;
}
