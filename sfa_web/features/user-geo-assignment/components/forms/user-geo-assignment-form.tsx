"use client";

import { useEffect, useState, useCallback } from "react";
import { useForm, Controller } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { CheckCircle2, MapPin } from "lucide-react";
import { AsyncSelect } from "@/components/async-select";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Spinner } from "@/components/ui/spinner";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import {
  Form,
  FormControl,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from "@/components/ui/form";
import {
  createUserGeoAssignmentSchema,
  updateUserGeoAssignmentSchema,
  type CreateUserGeoAssignmentInput,
  type UpdateUserGeoAssignmentInput,
} from "../../schema/user-geo-assignment.schema";
import {
  useUsersForGeoSelect,
  useRegionsForSelect,
  useAreasForSelect,
  useTerritoriesForSelect,
  useDivisionsForSelect,
} from "../../hooks/user-geo-assignment.hooks";
import type { UserDto } from "@/features/user/schema/user.schema";
import type { RegionDto } from "@/features/region/schema/region.schema";
import type { AreaDto } from "@/features/area/schema/area.schema";
import type { TerritoryDto } from "@/features/territory/schema/territory.schema";
import type { DivisionDto } from "@/features/division/schema/division.schema";

// ── Constants ────────────────────────────────────────────────────────────────

const ASSIGNABLE_ROLES = ["NSM", "RSM", "ASM", "Supervisor", "SalesRep"];

const roleBadgeClass: Record<string, string> = {
  NSM: "bg-blue-100 text-blue-700 border-blue-200",
  RSM: "bg-purple-100 text-purple-700 border-purple-200",
  ASM: "bg-indigo-100 text-indigo-700 border-indigo-200",
  Supervisor: "bg-orange-100 text-orange-700 border-orange-200",
  SalesRep: "bg-green-100 text-green-700 border-green-200",
  Admin: "bg-red-100 text-red-700 border-red-200",
};

// ── Small helpers ─────────────────────────────────────────────────────────────

function getInitials(name: string) {
  return name
    .split(" ")
    .slice(0, 2)
    .map((n) => n[0])
    .join("")
    .toUpperCase();
}

function RoleBadge({ role }: { role: string }) {
  const cls =
    roleBadgeClass[role] ?? "bg-muted text-muted-foreground border-border";
  return (
    <Badge variant="outline" className={`text-xs font-medium ${cls}`}>
      {role}
    </Badge>
  );
}

function UserPreviewCard({ user }: { user: UserDto }) {
  return (
    <div className="flex items-center gap-3 rounded-lg border bg-muted/40 px-4 py-3">
      <div className="flex h-9 w-9 shrink-0 items-center justify-center rounded-full bg-primary/10 text-sm font-semibold text-primary">
        {getInitials(user.name)}
      </div>
      <div>
        <p className="text-sm font-medium leading-none">{user.name}</p>
        <div className="mt-1">
          <RoleBadge role={user.role} />
        </div>
      </div>
    </div>
  );
}

function UserOption({ user }: { user: UserDto }) {
  return (
    <div className="flex items-center gap-2 py-0.5">
      <div className="flex h-6 w-6 shrink-0 items-center justify-center rounded-full bg-muted text-[10px] font-semibold text-muted-foreground">
        {getInitials(user.name)}
      </div>
      <div className="flex flex-col">
        <span className="text-sm leading-none">{user.name}</span>
        <span className="text-xs text-muted-foreground">{user.role}</span>
      </div>
    </div>
  );
}

function DivisionPreviewCard({ division }: { division: DivisionDto }) {
  return (
    <div className="flex items-start gap-3 rounded-lg border bg-muted/40 px-4 py-3">
      <MapPin className="mt-0.5 h-4 w-4 shrink-0 text-orange-500" />
      <div>
        <p className="text-[10px] font-semibold uppercase tracking-wide text-muted-foreground">
          Division
        </p>
        <p className="text-sm font-medium leading-snug">{division.name}</p>
        <p className="mt-0.5 text-xs text-muted-foreground">
          {division.regionName} → {division.areaName} → {division.territoryName}
        </p>
      </div>
    </div>
  );
}

// ── Geo-level stepper ─────────────────────────────────────────────────────────

const GEO_STEPS = ["Region", "Area", "Territory", "Division"] as const;
type GeoStep = (typeof GEO_STEPS)[number];

function GeoStepper({ activeStep }: { activeStep: GeoStep }) {
  const activeIdx = GEO_STEPS.indexOf(activeStep);
  return (
    <div className="flex items-center gap-1.5">
      {GEO_STEPS.map((step, i) => {
        const done = i < activeIdx;
        const active = i === activeIdx;
        return (
          <div key={step} className="flex items-center gap-1.5">
            {i > 0 && <div className="h-px w-3 bg-border" />}
            <span
              className={`inline-flex items-center gap-1 rounded-full px-2.5 py-0.5 text-xs font-medium transition-colors ${
                active
                  ? "bg-orange-500 text-white"
                  : done
                    ? "bg-primary/10 text-primary"
                    : "bg-muted text-muted-foreground"
              }`}
            >
              {done && <CheckCircle2 className="h-3 w-3" />}
              {step}
            </span>
          </div>
        );
      })}
    </div>
  );
}

// ── Fetcher hooks (backed by TanStack Query cache) ────────────────────────────

function useSubordinateFetcher(users: UserDto[]) {
  return useCallback(
    async (query?: string): Promise<UserDto[]> => {
      if (!query) return [];
      const pool = users.filter(
        (u) => ASSIGNABLE_ROLES.includes(u.role) && u.isActive,
      );
      return pool.filter((u) =>
        u.name.toLowerCase().includes(query.toLowerCase()),
      );
    },
    [users],
  );
}

// ── Interfaces ────────────────────────────────────────────────────────────────

interface CreateFormProps {
  mode: "create";
  defaultValues?: undefined;
  onSubmit: (data: CreateUserGeoAssignmentInput) => void;
  onCancel?: () => void;
  isLoading: boolean;
  fieldErrors?: Record<string, string> | null;
}

interface EditFormProps {
  mode: "edit";
  defaultValues?: Partial<UpdateUserGeoAssignmentInput> & {
    userName?: string;
    userRole?: string;
    reportsToUserId?: number; // read-only display
    regionId?: number;
    areaId?: number;
    territoryId?: number;
  };
  onSubmit: (data: UpdateUserGeoAssignmentInput) => void;
  onCancel?: () => void;
  isLoading: boolean;
  fieldErrors?: Record<string, string> | null;
}

export type UserGeoAssignmentFormProps = CreateFormProps | EditFormProps;

// ── Main export ───────────────────────────────────────────────────────────────

export function UserGeoAssignmentForm(props: UserGeoAssignmentFormProps) {
  const { data: users = [], isLoading: isLoadingUsers } =
    useUsersForGeoSelect();
  const { data: regions = [], isLoading: isLoadingRegions } =
    useRegionsForSelect();
  const { data: areas = [], isLoading: isLoadingAreas } = useAreasForSelect();
  const { data: territories = [], isLoading: isLoadingTerritories } =
    useTerritoriesForSelect();
  const { data: divisions = [], isLoading: isLoadingDivisions } =
    useDivisionsForSelect();

  const isLoadingGeo =
    isLoadingRegions ||
    isLoadingAreas ||
    isLoadingTerritories ||
    isLoadingDivisions;

  if (props.mode === "create") {
    return (
      <CreateForm
        {...props}
        users={users}
        isLoadingUsers={isLoadingUsers}
        regions={regions}
        areas={areas}
        territories={territories}
        divisions={divisions}
        isLoadingGeo={isLoadingGeo}
      />
    );
  }
  return (
    <EditForm
      {...props}
      users={users}
      isLoadingUsers={isLoadingUsers}
      regions={regions}
      areas={areas}
      territories={territories}
      divisions={divisions}
      isLoadingGeo={isLoadingGeo}
    />
  );
}

// ── Shared geo cascade section ────────────────────────────────────────────────

interface GeoCascadeProps {
  selectedRegionId: number;
  selectedAreaId: number;
  selectedTerritoryId: number;
  selectedDivisionId: number;
  onRegionChange: (id: number) => void;
  onAreaChange: (id: number) => void;
  onTerritoryChange: (id: number) => void;
  onDivisionChange: (id: number) => void;
  regions: RegionDto[];
  areas: AreaDto[];
  territories: TerritoryDto[];
  divisions: DivisionDto[];
  isLoadingGeo: boolean;
  divisionError?: string;
}

function GeoCascadeSection({
  selectedRegionId,
  selectedAreaId,
  selectedTerritoryId,
  selectedDivisionId,
  onRegionChange,
  onAreaChange,
  onTerritoryChange,
  onDivisionChange,
  regions,
  areas,
  territories,
  divisions,
  isLoadingGeo,
  divisionError,
}: GeoCascadeProps) {
  const filteredAreas = selectedRegionId
    ? areas.filter((a) => a.regionId === selectedRegionId)
    : [];
  const filteredTerritories = selectedAreaId
    ? territories.filter((t) => t.areaId === selectedAreaId)
    : [];
  const filteredDivisions = selectedTerritoryId
    ? divisions.filter((d) => d.territoryId === selectedTerritoryId)
    : [];

  const selectedDivision =
    divisions.find((d) => d.id === selectedDivisionId) ?? null;

  const activeStep: GeoStep = selectedTerritoryId
    ? "Division"
    : selectedAreaId
      ? "Territory"
      : selectedRegionId
        ? "Area"
        : "Region";

  return (
    <div className="space-y-3">
      <div className="space-y-1">
        <p className="text-xs font-semibold uppercase tracking-wide text-muted-foreground">
          Geographic Area{" "}
          <span className="text-muted-foreground text-xs font-normal">
            (optional)
          </span>
        </p>
        <GeoStepper activeStep={activeStep} />
      </div>

      <div className="grid grid-cols-2 gap-3">
        {/* Region */}
        <div className="space-y-1">
          <p className="text-xs text-muted-foreground">Region</p>
          <Select
            disabled={isLoadingGeo}
            value={selectedRegionId ? String(selectedRegionId) : ""}
            onValueChange={(v) => onRegionChange(Number(v))}
          >
            <SelectTrigger className="w-full">
              <SelectValue placeholder="Select region" />
            </SelectTrigger>
            <SelectContent>
              {regions.map((r) => (
                <SelectItem key={r.id} value={String(r.id)}>
                  {r.name}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>

        {/* Area */}
        <div className="space-y-1">
          <p className="text-xs text-muted-foreground">Area</p>
          <Select
            disabled={isLoadingGeo || !selectedRegionId}
            value={selectedAreaId ? String(selectedAreaId) : ""}
            onValueChange={(v) => onAreaChange(Number(v))}
          >
            <SelectTrigger className="w-full">
              <SelectValue
                placeholder={selectedRegionId ? "Select area" : "—"}
              />
            </SelectTrigger>
            <SelectContent>
              {filteredAreas.map((a) => (
                <SelectItem key={a.id} value={String(a.id)}>
                  {a.name}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>

        {/* Territory */}
        <div className="space-y-1">
          <p className="text-xs text-muted-foreground">Territory</p>
          <Select
            disabled={isLoadingGeo || !selectedAreaId}
            value={selectedTerritoryId ? String(selectedTerritoryId) : ""}
            onValueChange={(v) => onTerritoryChange(Number(v))}
          >
            <SelectTrigger className="w-full">
              <SelectValue
                placeholder={selectedAreaId ? "Select territory" : "—"}
              />
            </SelectTrigger>
            <SelectContent>
              {filteredTerritories.map((t) => (
                <SelectItem key={t.id} value={String(t.id)}>
                  {t.name}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>

        {/* Division */}
        <div className="space-y-1">
          <p className="text-xs text-muted-foreground">Division</p>
          <Select
            disabled={isLoadingGeo || !selectedTerritoryId}
            value={selectedDivisionId ? String(selectedDivisionId) : ""}
            onValueChange={(v) => onDivisionChange(Number(v))}
          >
            <SelectTrigger className="w-full">
              <SelectValue
                placeholder={selectedTerritoryId ? "Select division" : "—"}
              />
            </SelectTrigger>
            <SelectContent>
              {filteredDivisions.map((d) => (
                <SelectItem key={d.id} value={String(d.id)}>
                  {d.name}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
          {divisionError && (
            <p className="text-xs text-destructive">{divisionError}</p>
          )}
        </div>

      </div>

      {selectedDivision && <DivisionPreviewCard division={selectedDivision} />}
    </div>
  );
}

// ── Create form ───────────────────────────────────────────────────────────────

function CreateForm({
  onSubmit,
  onCancel,
  isLoading,
  fieldErrors,
  users,
  isLoadingUsers,
  regions,
  areas,
  territories,
  divisions,
  isLoadingGeo,
}: Omit<CreateFormProps, "mode"> & {
  users: UserDto[];
  isLoadingUsers: boolean;
  regions: RegionDto[];
  areas: AreaDto[];
  territories: TerritoryDto[];
  divisions: DivisionDto[];
  isLoadingGeo: boolean;
}) {
  const form = useForm<CreateUserGeoAssignmentInput>({
    resolver: zodResolver(createUserGeoAssignmentSchema),
    defaultValues: {
      userId: 0,
      regionId: undefined,
      areaId: undefined,
      territoryId: undefined,
      divisionId: undefined,
      effectiveFrom: new Date().toISOString().split("T")[0],
    },
  });

  const { setError, setValue, watch } = form;
  const userId = watch("userId");
  const divisionId = watch("divisionId");

  const [selectedRegionId, setSelectedRegionId] = useState(0);
  const [selectedAreaId, setSelectedAreaId] = useState(0);
  const [selectedTerritoryId, setSelectedTerritoryId] = useState(0);

  const selectedUser = users.find((u) => u.id === userId);
  const subordinateFetcher = useSubordinateFetcher(users);

  function handleRegionChange(id: number) {
    setSelectedRegionId(id);
    setSelectedAreaId(0);
    setSelectedTerritoryId(0);
    setValue("regionId", id > 0 ? id : undefined);
    setValue("areaId", undefined);
    setValue("territoryId", undefined);
    setValue("divisionId", undefined);
  }

  function handleAreaChange(id: number) {
    setSelectedAreaId(id);
    setSelectedTerritoryId(0);
    setValue("areaId", id > 0 ? id : undefined);
    setValue("territoryId", undefined);
    setValue("divisionId", undefined);
  }

  function handleTerritoryChange(id: number) {
    setSelectedTerritoryId(id);
    setValue("territoryId", id > 0 ? id : undefined);
    setValue("divisionId", undefined);
  }

  function handleDivisionChange(id: number) {
    setValue("divisionId", id > 0 ? id : undefined);
  }

  useEffect(() => {
    if (fieldErrors) {
      Object.entries(fieldErrors).forEach(([field, message]) => {
        setError(field as keyof CreateUserGeoAssignmentInput, { message });
      });
    }
  }, [fieldErrors, setError]);

  return (
    <Form {...form}>
      <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
        {/* USER */}
        <div className="space-y-2">
          <p className="text-xs font-semibold uppercase tracking-wide text-muted-foreground">
            User <span className="text-destructive">*</span>
          </p>
          <Controller
            control={form.control}
            name="userId"
            render={({ field, fieldState }) => (
              <div className="space-y-1">
                <AsyncSelect<UserDto>
                  fetcher={subordinateFetcher}
                  preload={false}
                  label="user"
                  placeholder="Type to search user…"
                  value={field.value > 0 ? String(field.value) : ""}
                  onChange={(v) => field.onChange(v ? Number(v) : 0)}
                  getOptionValue={(u) => String(u.id)}
                  getDisplayValue={(u) => (
                    <span>
                      {u.name} — {u.role}
                    </span>
                  )}
                  renderOption={(u) => <UserOption user={u} />}
                  noResultsMessage="Type to search…"
                  disabled={isLoadingUsers}
                  width="100%"
                  triggerClassName="w-full"
                />
                {fieldState.error && (
                  <p className="text-xs text-destructive">
                    {fieldState.error.message}
                  </p>
                )}
              </div>
            )}
          />
          {selectedUser && <UserPreviewCard user={selectedUser} />}
        </div>

        {/* GEOGRAPHIC AREA */}
        <GeoCascadeSection
          selectedRegionId={selectedRegionId}
          selectedAreaId={selectedAreaId}
          selectedTerritoryId={selectedTerritoryId}
          selectedDivisionId={divisionId ?? 0}
          onRegionChange={handleRegionChange}
          onAreaChange={handleAreaChange}
          onTerritoryChange={handleTerritoryChange}
          onDivisionChange={handleDivisionChange}
          regions={regions}
          areas={areas}
          territories={territories}
          divisions={divisions}
          isLoadingGeo={isLoadingGeo}
        />

        {/* EFFECTIVE FROM */}
        <FormField
          control={form.control}
          name="effectiveFrom"
          render={({ field }) => (
            <FormItem>
              <FormLabel className="text-xs font-semibold uppercase tracking-wide text-muted-foreground">
                Effective From <span className="text-destructive">*</span>
              </FormLabel>
              <FormControl>
                <Input type="date" className="w-full" {...field} />
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />

        <div className="flex justify-end gap-3 pt-1">
          {onCancel && (
            <Button
              type="button"
              variant="outline"
              onClick={onCancel}
              disabled={isLoading}
            >
              Cancel
            </Button>
          )}
          <Button
            type="submit"
            disabled={isLoading || isLoadingUsers || isLoadingGeo}
            className="bg-orange-500 hover:bg-orange-600 text-white"
          >
            {isLoading ? <Spinner className="mr-2" /> : null}
            Save assignment
          </Button>
        </div>
      </form>
    </Form>
  );
}

// ── Edit form ─────────────────────────────────────────────────────────────────

function EditForm({
  defaultValues,
  onSubmit,
  onCancel,
  isLoading,
  fieldErrors,
  users,
  isLoadingUsers,
  regions,
  areas,
  territories,
  divisions,
  isLoadingGeo,
}: Omit<EditFormProps, "mode"> & {
  users: UserDto[];
  isLoadingUsers: boolean;
  regions: RegionDto[];
  areas: AreaDto[];
  territories: TerritoryDto[];
  divisions: DivisionDto[];
  isLoadingGeo: boolean;
}) {
  // Resolve initial cascade state: prefer division ancestors, fall back to explicit IDs
  const existingDivision = divisions.find(
    (d) => d.id === defaultValues?.divisionId,
  );
  const initRegionId =
    existingDivision?.regionId ?? defaultValues?.regionId ?? 0;
  const initAreaId = existingDivision?.areaId ?? defaultValues?.areaId ?? 0;
  const initTerritoryId =
    existingDivision?.territoryId ?? defaultValues?.territoryId ?? 0;

  const [selectedRegionId, setSelectedRegionId] = useState(initRegionId);
  const [selectedAreaId, setSelectedAreaId] = useState(initAreaId);
  const [selectedTerritoryId, setSelectedTerritoryId] =
    useState(initTerritoryId);

  const form = useForm<UpdateUserGeoAssignmentInput>({
    resolver: zodResolver(updateUserGeoAssignmentSchema),
    defaultValues: {
      regionId: initRegionId > 0 ? initRegionId : undefined,
      areaId: initAreaId > 0 ? initAreaId : undefined,
      territoryId: initTerritoryId > 0 ? initTerritoryId : undefined,
      divisionId: defaultValues?.divisionId ?? undefined,
      effectiveFrom:
        defaultValues?.effectiveFrom ?? new Date().toISOString().split("T")[0],
    },
  });

  const { setError, setValue, watch } = form;
  const divisionId = watch("divisionId");

  // Sync cascade state once division data loads (handles late-loading divisions list)
  useEffect(() => {
    if (existingDivision && !selectedRegionId) {
      setSelectedRegionId(existingDivision.regionId);
      setSelectedAreaId(existingDivision.areaId);
      setSelectedTerritoryId(existingDivision.territoryId);
      setValue("regionId", existingDivision.regionId);
      setValue("areaId", existingDivision.areaId);
      setValue("territoryId", existingDivision.territoryId);
    }
  }, [existingDivision, selectedRegionId, setValue]);

  function handleRegionChange(id: number) {
    setSelectedRegionId(id);
    setSelectedAreaId(0);
    setSelectedTerritoryId(0);
    setValue("regionId", id > 0 ? id : undefined);
    setValue("areaId", undefined);
    setValue("territoryId", undefined);
    setValue("divisionId", undefined);
  }

  function handleAreaChange(id: number) {
    setSelectedAreaId(id);
    setSelectedTerritoryId(0);
    setValue("areaId", id > 0 ? id : undefined);
    setValue("territoryId", undefined);
    setValue("divisionId", undefined);
  }

  function handleTerritoryChange(id: number) {
    setSelectedTerritoryId(id);
    setValue("territoryId", id > 0 ? id : undefined);
    setValue("divisionId", undefined);
  }

  function handleDivisionChange(id: number) {
    setValue("divisionId", id > 0 ? id : undefined);
  }

  useEffect(() => {
    if (fieldErrors) {
      Object.entries(fieldErrors).forEach(([field, message]) => {
        setError(field as keyof UpdateUserGeoAssignmentInput, { message });
      });
    }
  }, [fieldErrors, setError]);

  // Read-only subordinate preview
  const subordinateName = defaultValues?.userName ?? "";
  const subordinateRole = defaultValues?.userRole ?? "";
  const fakeSubordinate = subordinateName
    ? ({
        id: 0,
        name: subordinateName,
        role: subordinateRole,
        isActive: true,
      } as UserDto)
    : null;

  return (
    <Form {...form}>
      <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
        {/* Subordinate preview (read-only) */}
        {fakeSubordinate && (
          <div className="space-y-2">
            <p className="text-xs font-semibold uppercase tracking-wide text-muted-foreground">
              User
            </p>
            <UserPreviewCard user={fakeSubordinate} />
          </div>
        )}

        {/* GEOGRAPHIC AREA */}
        <GeoCascadeSection
          selectedRegionId={selectedRegionId}
          selectedAreaId={selectedAreaId}
          selectedTerritoryId={selectedTerritoryId}
          selectedDivisionId={divisionId ?? 0}
          onRegionChange={handleRegionChange}
          onAreaChange={handleAreaChange}
          onTerritoryChange={handleTerritoryChange}
          onDivisionChange={handleDivisionChange}
          regions={regions}
          areas={areas}
          territories={territories}
          divisions={divisions}
          isLoadingGeo={isLoadingGeo}
        />

        {/* EFFECTIVE FROM */}
        <FormField
          control={form.control}
          name="effectiveFrom"
          render={({ field }) => (
            <FormItem>
              <FormLabel className="text-xs font-semibold uppercase tracking-wide text-muted-foreground">
                Effective From <span className="text-destructive">*</span>
              </FormLabel>
              <FormControl>
                <Input type="date" className="w-full" {...field} />
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />

        <div className="flex justify-end gap-3 pt-1">
          {onCancel && (
            <Button
              type="button"
              variant="outline"
              onClick={onCancel}
              disabled={isLoading}
            >
              Cancel
            </Button>
          )}
          <Button
            type="submit"
            disabled={isLoading || isLoadingUsers || isLoadingGeo}
            className="bg-orange-500 hover:bg-orange-600 text-white"
          >
            {isLoading ? <Spinner className="mr-2" /> : null}
            Save assignment
          </Button>
        </div>
      </form>
    </Form>
  );
}
