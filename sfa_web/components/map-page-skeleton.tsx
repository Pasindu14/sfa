import { Skeleton } from "@/components/ui/skeleton";

/**
 * Placeholder shown while a Google Maps page's chunk (map library + page code) downloads.
 * Mirrors the map pages' layout — padded column, hero header card, full-height map panel —
 * so nothing jumps when the real page swaps in.
 */
export function MapPageSkeleton({
  mapHeight = "calc(100vh - 260px)",
  withFilterBar = false,
}: {
  mapHeight?: string;
  withFilterBar?: boolean;
}) {
  return (
    <div className="flex flex-col gap-6 p-6" aria-busy="true" aria-live="polite">
      <div className="rounded-lg bg-muted/90 p-10">
        <Skeleton className="h-9 w-64 bg-background/60" />
        <Skeleton className="mt-2 h-5 w-80 bg-background/60" />
      </div>
      {withFilterBar && <Skeleton className="h-[74px] w-full rounded-lg" />}
      <Skeleton className="w-full rounded-xl" style={{ height: mapHeight }} />
    </div>
  );
}
