'use client'

import {
  Globe,
  Map,
  MapPin,
  Building2,
  Route,
  Users,
  ToggleLeft,
  RotateCcw,
  Search,
  Lock,
  Loader2,
} from 'lucide-react'
import { Button } from '@/components/ui/button'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { Badge } from '@/components/ui/badge'
import { useUserGeoAssignmentFilters } from '../../store'
import {
  useRegionsForSelect,
  useAreasForSelect,
  useTerritoriesForSelect,
  useDivisionsForSelect,
  useRoutesForDivision,
} from '../../hooks/user-geo-assignment.hooks'

const ROLES = ['NSM', 'RSM', 'ASM', 'Supervisor', 'SalesRep']

const roleBadgeClass: Record<string, string> = {
  NSM: 'bg-blue-100 text-blue-700 border-blue-200',
  RSM: 'bg-purple-100 text-purple-700 border-purple-200',
  ASM: 'bg-indigo-100 text-indigo-700 border-indigo-200',
  Supervisor: 'bg-orange-100 text-orange-700 border-orange-200',
  SalesRep: 'bg-green-100 text-green-700 border-green-200',
}

interface GeoStepProps {
  icon: React.ReactNode
  label: string
  locked?: boolean
  children: React.ReactNode
}

function GeoStep({ icon, label, locked, children }: GeoStepProps) {
  return (
    <div className="flex flex-col gap-1.5 min-w-0 flex-1">
      <div className="flex items-center gap-1.5">
        <span className={locked ? 'text-muted-foreground/40' : 'text-orange-500'}>{icon}</span>
        <span className={`text-xs font-semibold tracking-wide uppercase ${locked ? 'text-muted-foreground/40' : 'text-foreground/70'}`}>
          {label}
        </span>
        {locked && <Lock className="h-2.5 w-2.5 text-muted-foreground/30" />}
      </div>
      {children}
    </div>
  )
}

export function GeoAssignmentFilterCard() {
  const { pending, committed, setPending, commit, reset } = useUserGeoAssignmentFilters()

  const { data: regions = [], isLoading: loadingRegions } = useRegionsForSelect()
  const { data: areas = [], isLoading: loadingAreas } = useAreasForSelect()
  const { data: territories = [], isLoading: loadingTerritories } = useTerritoriesForSelect()
  const { data: divisions = [], isLoading: loadingDivisions } = useDivisionsForSelect()

  const filteredAreas = pending.regionId
    ? areas.filter((a) => a.regionId === pending.regionId)
    : areas
  const filteredTerritories = pending.areaId
    ? territories.filter((t) => t.areaId === pending.areaId)
    : territories
  const filteredDivisions = pending.territoryId
    ? divisions.filter((d) => d.territoryId === pending.territoryId)
    : divisions

  const isLoading = loadingRegions || loadingAreas || loadingTerritories || loadingDivisions
  const hasCommitted = committed !== null

  const activeFilterCount = [
    pending.role,
    pending.regionId,
    pending.areaId,
    pending.territoryId,
    pending.divisionId,
    pending.routeId,
    pending.isActive,
  ].filter(Boolean).length

  const { data: routes = [], isLoading: loadingRoutes } = useRoutesForDivision(
    pending.divisionId || undefined,
  )

  function handleRegionChange(value: string) {
    const regionId = value === '__all__' ? 0 : Number(value)
    setPending({ regionId, areaId: 0, territoryId: 0, divisionId: 0, routeId: 0 })
  }

  function handleAreaChange(value: string) {
    const areaId = value === '__all__' ? 0 : Number(value)
    setPending({ areaId, territoryId: 0, divisionId: 0, routeId: 0 })
  }

  function handleTerritoryChange(value: string) {
    const territoryId = value === '__all__' ? 0 : Number(value)
    setPending({ territoryId, divisionId: 0, routeId: 0 })
  }

  function handleDivisionChange(value: string) {
    const divisionId = value === '__all__' ? 0 : Number(value)
    setPending({ divisionId, routeId: 0 })
  }

  return (
    <div className="overflow-hidden rounded-xl border bg-card shadow-sm">
      {/* Card header */}
      <div className="flex items-center justify-between border-b bg-muted/30 px-5 py-3.5">
        <div className="flex items-center gap-2.5">
          <div className="flex h-7 w-7 items-center justify-center rounded-md bg-orange-100">
            <MapPin className="h-3.5 w-3.5 text-orange-600" />
          </div>
          <div>
            <span className="text-sm font-semibold">Filter Assignments</span>
            <p className="text-[11px] text-muted-foreground leading-tight">
              Narrow down by role, status, or drill into the geo hierarchy
            </p>
          </div>
          {activeFilterCount > 0 && (
            <Badge className="h-5 rounded-full bg-orange-500 px-2 text-[10px] text-white">
              {activeFilterCount} active
            </Badge>
          )}
        </div>
        {activeFilterCount > 0 && (
          <Button
            variant="ghost"
            size="sm"
            onClick={reset}
            className="h-7 gap-1.5 text-xs text-muted-foreground hover:text-foreground"
          >
            <RotateCcw className="h-3 w-3" />
            Clear all
          </Button>
        )}
      </div>

      <div className="p-5 space-y-4">
        {/* Single row: all filters */}
        <div className="grid grid-cols-2 gap-3 sm:grid-cols-4 lg:grid-cols-7">
          {/* Role */}
          <div className="flex flex-col gap-1.5">
            <div className="flex items-center gap-1.5">
              <Users className="h-3.5 w-3.5 text-purple-500" />
              <span className="text-xs font-semibold uppercase tracking-wide text-foreground/70">Role</span>
            </div>
            <Select
              value={pending.role || '__all__'}
              onValueChange={(v) => setPending({ role: v === '__all__' ? '' : v })}
            >
              <SelectTrigger className="h-9 w-full text-sm">
                <SelectValue placeholder="All roles" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="__all__">All roles</SelectItem>
                {ROLES.map((r) => (
                  <SelectItem key={r} value={r}>
                    <span className="flex items-center gap-2">
                      <span className={`inline-block h-2 w-2 rounded-full ${roleBadgeClass[r]?.split(' ')[0] ?? 'bg-muted'}`} />
                      {r}
                    </span>
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>

          {/* Status */}
          <div className="flex flex-col gap-1.5">
            <div className="flex items-center gap-1.5">
              <ToggleLeft className="h-3.5 w-3.5 text-emerald-500" />
              <span className="text-xs font-semibold uppercase tracking-wide text-foreground/70">Status</span>
            </div>
            <Select
              value={pending.isActive || '__all__'}
              onValueChange={(v) => setPending({ isActive: v === '__all__' ? '' : v })}
            >
              <SelectTrigger className="h-9 w-full text-sm">
                <SelectValue placeholder="All status" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="__all__">All status</SelectItem>
                <SelectItem value="true">
                  <span className="flex items-center gap-2">
                    <span className="inline-block h-2 w-2 rounded-full bg-emerald-500" />
                    Active only
                  </span>
                </SelectItem>
                <SelectItem value="false">
                  <span className="flex items-center gap-2">
                    <span className="inline-block h-2 w-2 rounded-full bg-muted-foreground/40" />
                    Inactive only
                  </span>
                </SelectItem>
              </SelectContent>
            </Select>
          </div>

          {/* Region */}
          <GeoStep icon={<Globe className="h-3.5 w-3.5" />} label="Region">
            <Select
              disabled={isLoading}
              value={pending.regionId ? String(pending.regionId) : '__all__'}
              onValueChange={handleRegionChange}
            >
              <SelectTrigger className="h-9 w-full text-sm">
                <SelectValue placeholder="All regions" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="__all__">All regions</SelectItem>
                {regions.map((r) => (
                  <SelectItem key={r.id} value={String(r.id)}>{r.name}</SelectItem>
                ))}
              </SelectContent>
            </Select>
          </GeoStep>

          {/* Area */}
          <GeoStep
            icon={<Map className="h-3.5 w-3.5" />}
            label="Area"
            locked={!pending.regionId}
          >
            <Select
              disabled={isLoading || !pending.regionId}
              value={pending.areaId ? String(pending.areaId) : '__all__'}
              onValueChange={handleAreaChange}
            >
              <SelectTrigger className="h-9 w-full text-sm">
                <SelectValue placeholder={pending.regionId ? 'All areas' : '—'} />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="__all__">All areas</SelectItem>
                {filteredAreas.map((a) => (
                  <SelectItem key={a.id} value={String(a.id)}>{a.name}</SelectItem>
                ))}
              </SelectContent>
            </Select>
          </GeoStep>

          {/* Territory */}
          <GeoStep
            icon={<MapPin className="h-3.5 w-3.5" />}
            label="Territory"
            locked={!pending.areaId}
          >
            <Select
              disabled={isLoading || !pending.areaId}
              value={pending.territoryId ? String(pending.territoryId) : '__all__'}
              onValueChange={handleTerritoryChange}
            >
              <SelectTrigger className="h-9 w-full text-sm">
                <SelectValue placeholder={pending.areaId ? 'All territories' : '—'} />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="__all__">All territories</SelectItem>
                {filteredTerritories.map((t) => (
                  <SelectItem key={t.id} value={String(t.id)}>{t.name}</SelectItem>
                ))}
              </SelectContent>
            </Select>
          </GeoStep>

          {/* Division */}
          <GeoStep
            icon={<Building2 className="h-3.5 w-3.5" />}
            label="Division"
            locked={!pending.territoryId}
          >
            <Select
              disabled={isLoading || !pending.territoryId}
              value={pending.divisionId ? String(pending.divisionId) : '__all__'}
              onValueChange={handleDivisionChange}
            >
              <SelectTrigger className="h-9 w-full text-sm">
                <SelectValue placeholder={pending.territoryId ? 'All divisions' : '—'} />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="__all__">All divisions</SelectItem>
                {filteredDivisions.map((d) => (
                  <SelectItem key={d.id} value={String(d.id)}>{d.name}</SelectItem>
                ))}
              </SelectContent>
            </Select>
          </GeoStep>

          {/* Route */}
          <GeoStep
            icon={
              loadingRoutes
                ? <Loader2 className="h-3.5 w-3.5 animate-spin" />
                : <Route className="h-3.5 w-3.5" />
            }
            label="Route"
            locked={!pending.divisionId}
          >
            <Select
              disabled={!pending.divisionId || loadingRoutes}
              value={pending.routeId ? String(pending.routeId) : '__all__'}
              onValueChange={(v) => setPending({ routeId: v === '__all__' ? 0 : Number(v) })}
            >
              <SelectTrigger className="h-9 w-full text-sm">
                <SelectValue placeholder={
                  loadingRoutes ? 'Loading…'
                  : pending.divisionId ? 'All routes'
                  : '—'
                } />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="__all__">All routes</SelectItem>
                {routes.map((r) => (
                  <SelectItem key={r.id} value={String(r.id)}>{r.name}</SelectItem>
                ))}
              </SelectContent>
            </Select>
          </GeoStep>
        </div>

        {/* Action row */}
        <div className="flex items-center justify-between border-t pt-4">
          <p className="text-xs text-muted-foreground">
            {hasCommitted
              ? 'Adjust filters above and click Load Results to refresh.'
              : 'Set your filters above, then click Load Results to fetch data.'}
          </p>
          <div className="flex items-center gap-2">
            {hasCommitted && (
              <Button variant="outline" size="sm" onClick={reset} className="gap-1.5">
                <RotateCcw className="h-3.5 w-3.5" />
                Reset
              </Button>
            )}
            <Button
              size="sm"
              onClick={commit}
              className="gap-1.5 bg-orange-500 hover:bg-orange-600 text-white"
            >
              <Search className="h-3.5 w-3.5" />
              Load Results
            </Button>
          </div>
        </div>
      </div>
    </div>
  )
}
