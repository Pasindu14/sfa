"use client";

import dynamic from "next/dynamic";
import { MapPageSkeleton } from "@/components/map-page-skeleton";

// The page (and its APIProvider) is loaded on demand so the Google Maps library stays out of
// the route's initial bundle.
const FieldRepsMapPage = dynamic(
  () =>
    import("@/features/field-rep/components/pages/field-reps-map-page").then((m) => ({
      default: m.FieldRepsMapPage,
    })),
  { ssr: false, loading: () => <MapPageSkeleton /> },
);

export default function FieldRepsLiveMapPage() {
  return <FieldRepsMapPage />;
}
