"use client";

import { useActiveRegions } from "../../hooks/region.hooks";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { Spinner } from "@/components/ui/spinner";

interface RegionSelectProps {
  value?: string;
  onValueChange: (value: string) => void;
  disabled?: boolean;
  placeholder?: string;
}

export function RegionSelect({
  value,
  onValueChange,
  disabled,
  placeholder = "Select region",
}: RegionSelectProps) {
  const { data: regions, isLoading } = useActiveRegions();

  return (
    <div className="flex items-center gap-2">
      <Select
        value={value}
        onValueChange={onValueChange}
        disabled={disabled || isLoading}
      >
        <SelectTrigger className="flex-1">
          <SelectValue placeholder={placeholder} />
        </SelectTrigger>
        <SelectContent className="max-h-10 overflow-y-scroll">
          {regions?.map((region) => (
            <SelectItem key={region.id} value={String(region.id)}>
              {region.name}
            </SelectItem>
          ))}
        </SelectContent>
      </Select>
      {isLoading && (
        <Spinner className="size-4 shrink-0 text-muted-foreground" />
      )}
    </div>
  );
}
