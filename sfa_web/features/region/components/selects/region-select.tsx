"use client";

import { useCallback } from "react";
import { useActiveRegions } from "../../hooks/region.hooks";
import { AsyncSelect } from "@/components/async-select";
import type { RegionDto } from "../../schema/region.schema";

interface RegionSelectProps {
  value?: string;
  onValueChange: (value: string) => void;
  disabled?: boolean;
  placeholder?: string;
}

export function RegionSelect({
  value = "",
  onValueChange,
  disabled,
  placeholder = "Select region",
}: RegionSelectProps) {
  const { data: regions = [], isLoading } = useActiveRegions();

  // In-memory fetcher backed by the TanStack Query cache — no API call per keystroke.
  // Returns every active region on open (empty query); filters by name as the user types.
  const fetcher = useCallback(
    async (query?: string): Promise<RegionDto[]> => {
      if (!query) return regions;
      const q = query.toLowerCase();
      return regions.filter((r) => r.name.toLowerCase().includes(q));
    },
    [regions],
  );

  // Pre-paint the trigger label in edit mode, where `value` (a region id) is
  // known before the options list resolves.
  const initialOption = value
    ? regions.find((r) => String(r.id) === value) ?? null
    : null;

  return (
    <AsyncSelect<RegionDto>
      fetcher={fetcher}
      preload={false}
      label="region"
      placeholder={placeholder}
      value={value}
      onChange={onValueChange}
      getOptionValue={(r) => String(r.id)}
      getDisplayValue={(r) => <span>{r.name}</span>}
      renderOption={(r) => <span>{r.name}</span>}
      noResultsMessage="No regions found"
      disabled={disabled || isLoading}
      width="100%"
      triggerClassName="w-full"
      initialOption={initialOption}
    />
  );
}
