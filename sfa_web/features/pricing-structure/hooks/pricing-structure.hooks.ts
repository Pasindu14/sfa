'use client'

import { useMemo, useState } from 'react'
import {
  useQuery,
  useMutation,
  useQueryClient,
  keepPreviousData,
  type QueryClient,
} from '@tanstack/react-query'
import { toast } from 'sonner'
import {
  getPricingStructuresAction,
  getPricingStructureByIdAction,
  createPricingStructureAction,
  updatePricingStructureAction,
  duplicatePricingStructureAction,
  setDefaultPricingStructureAction,
  activatePricingStructureAction,
  deactivatePricingStructureAction,
  deletePricingStructureAction,
  getPricingStructureItemsAction,
  updatePricingStructureItemsAction,
  getDefaultStructurePricesAction,
} from '../actions/pricing-structure.actions'
import {
  useCreateDialog,
  useEditDialog,
  useDuplicateDialog,
  useSetDefaultDialog,
  useActivateDialog,
  useDeactivateDialog,
  useDeleteDialog,
} from '../store'
import { handleErrorToast } from '@/lib/hooks/use-error-toast'
import type { ActionFailure } from '@/lib/types/actions'
import type {
  CreatePricingStructureInput,
  UpdatePricingStructureInput,
  DuplicatePricingStructureInput,
  PricingStructureItemUpdate,
  PricingStructureItemRow,
  DefaultStructurePrice,
} from '../schema/pricing-structure.schema'
import { ActionError } from '@/lib/actions/action-error'

const RESOURCE = 'pricing structure'

// --- Query key factory ---

export const pricingStructureKeys = {
  all: ['pricing-structures'] as const,
  lists: () => [...pricingStructureKeys.all, 'list'] as const,
  list: (filters: object) => [...pricingStructureKeys.lists(), filters] as const,
  details: () => [...pricingStructureKeys.all, 'detail'] as const,
  detail: (id: number) => [...pricingStructureKeys.details(), id] as const,
  items: (id: number) => [...pricingStructureKeys.all, 'items', id] as const,
  defaultPrices: () => [...pricingStructureKeys.all, 'default-prices'] as const,
}

// Narrow invalidation: lists always (pricedCount, status, default flag and ordering all show
// there), plus only the details a mutation can actually have changed. The items grid and the
// default-price lookup are refreshed only by the mutations that move them.
function invalidateLists(queryClient: QueryClient) {
  queryClient.invalidateQueries({ queryKey: pricingStructureKeys.lists() })
}

function invalidateStructure(queryClient: QueryClient, id: number) {
  invalidateLists(queryClient)
  queryClient.invalidateQueries({ queryKey: pricingStructureKeys.detail(id) })
}

// --- Query hooks ---

export function usePricingStructure(id: number | null) {
  return useQuery({
    queryKey: pricingStructureKeys.detail(id!),
    queryFn: async () => {
      const result = await getPricingStructureByIdAction(id!)
      if (!result.success) throw new ActionError(result)
      return result.data
    },
    enabled: id !== null,
  })
}

export function usePricingStructureItems(id: number | null) {
  return useQuery({
    queryKey: pricingStructureKeys.items(id!),
    queryFn: async () => {
      const result = await getPricingStructureItemsAction(id!)
      if (!result.success) throw new ActionError(result)
      return result.data
    },
    enabled: id !== null,
  })
}

/**
 * The default structure's prices, keyed by productId — the staff PO editors price lines from
 * this. `null` data means no default structure exists yet (API 404), which the editors show as
 * "No pricing" rather than an error.
 */
export function useDefaultStructurePrices() {
  const query = useQuery({
    queryKey: pricingStructureKeys.defaultPrices(),
    queryFn: async () => {
      const result = await getDefaultStructurePricesAction()
      if (!result.success) {
        if (result.status === 404) return null
        throw new ActionError(result)
      }
      return result.data
    },
    staleTime: 5 * 60 * 1000, // 5 min — prices change infrequently
  })

  const pricesByProduct = useMemo(() => {
    const map = new Map<number, DefaultStructurePrice>()
    for (const item of query.data?.items ?? []) map.set(item.productId, item)
    return map
  }, [query.data])

  return { ...query, pricesByProduct }
}

// --- DataTable hook ---
// Uses server-side pagination + search — the API does all filtering.

export function usePricingStructureDataTable(
  page: number,
  pageSize: number,
  search: string,
  _dateRange?: { from_date: string; to_date: string },
  _sortBy?: string,
  _sortOrder?: string,
  _caseConfig?: unknown,
  _customFilters?: Record<string, unknown>,
) {
  return useQuery({
    queryKey: pricingStructureKeys.list({ page, pageSize, search }),
    queryFn: async () => {
      const result = await getPricingStructuresAction(page, pageSize, search || undefined)
      if (!result.success) throw new ActionError(result)
      const { pricingStructures, totalCount, page: p, pageSize: ps } = result.data
      return {
        success: true as const,
        data: pricingStructures,
        pagination: {
          page: p,
          limit: ps,
          total_pages: Math.ceil(totalCount / ps),
          total_items: totalCount,
        },
      }
    },
    placeholderData: keepPreviousData,
  })
}

;(usePricingStructureDataTable as unknown as Record<string, unknown>).isQueryHook = true

// --- Mutation hooks ---

export function useCreatePricingStructure() {
  const queryClient = useQueryClient()
  const { close } = useCreateDialog()
  const [fieldErrors, setFieldErrors] = useState<Record<string, string> | null>(null)

  const mutation = useMutation({
    mutationFn: async (data: CreatePricingStructureInput) => {
      const result = await createPricingStructureAction(data)
      if (!result.success) throw result
      return result.data
    },
    onSuccess: (created) => {
      invalidateLists(queryClient)
      // The very first structure becomes the active default — the PO editors can price from it now.
      if (created.isDefault)
        queryClient.invalidateQueries({ queryKey: pricingStructureKeys.defaultPrices() })
      setFieldErrors(null)
      close()
      toast.success('Pricing structure created successfully')
    },
    onError: (error: ActionFailure) => {
      if (error.fields) setFieldErrors(error.fields)
      handleErrorToast(error, RESOURCE, 'create')
    },
  })

  return { ...mutation, fieldErrors, clearFieldErrors: () => setFieldErrors(null) }
}

export function useUpdatePricingStructure() {
  const queryClient = useQueryClient()
  const { close } = useEditDialog()
  const [fieldErrors, setFieldErrors] = useState<Record<string, string> | null>(null)

  const mutation = useMutation({
    mutationFn: async ({ id, data }: { id: number; data: UpdatePricingStructureInput }) => {
      const result = await updatePricingStructureAction(id, data)
      if (!result.success) throw result
      return result.data
    },
    onSuccess: (updated, { id }) => {
      invalidateStructure(queryClient, id)
      // The default-price payload carries the structure name.
      if (updated.isDefault)
        queryClient.invalidateQueries({ queryKey: pricingStructureKeys.defaultPrices() })
      setFieldErrors(null)
      close()
      toast.success('Pricing structure updated successfully')
    },
    onError: (error: ActionFailure) => {
      if (error.fields) setFieldErrors(error.fields)
      handleErrorToast(error, RESOURCE, 'update')
    },
  })

  return { ...mutation, fieldErrors, clearFieldErrors: () => setFieldErrors(null) }
}

export function useDuplicatePricingStructure() {
  const queryClient = useQueryClient()
  const { close } = useDuplicateDialog()
  const [fieldErrors, setFieldErrors] = useState<Record<string, string> | null>(null)

  const mutation = useMutation({
    mutationFn: async ({ id, data }: { id: number; data: DuplicatePricingStructureInput }) => {
      const result = await duplicatePricingStructureAction(id, data)
      if (!result.success) throw result
      return result.data
    },
    onSuccess: (copy) => {
      invalidateLists(queryClient)
      setFieldErrors(null)
      close()
      toast.success(`Created "${copy.name}" — it starts inactive`)
    },
    onError: (error: ActionFailure) => {
      if (error.fields) setFieldErrors(error.fields)
      handleErrorToast(error, RESOURCE, 'duplicate')
    },
  })

  return { ...mutation, fieldErrors, clearFieldErrors: () => setFieldErrors(null) }
}

export function useSetDefaultPricingStructure() {
  const queryClient = useQueryClient()
  const { close } = useSetDefaultDialog()

  return useMutation({
    mutationFn: async ({ id, rowVersion }: { id: number; rowVersion: number }) => {
      const result = await setDefaultPricingStructureAction(id, rowVersion)
      if (!result.success) throw result
      return result.data
    },
    onSuccess: () => {
      // Two structures flip (old default off, new one on), so every cached detail is suspect.
      invalidateLists(queryClient)
      queryClient.invalidateQueries({ queryKey: pricingStructureKeys.details() })
      queryClient.invalidateQueries({ queryKey: pricingStructureKeys.defaultPrices() })
      close()
      toast.success('Default pricing structure changed')
    },
    onError: (error: ActionFailure) => {
      handleErrorToast(error, RESOURCE, 'set default')
    },
  })
}

export function useActivatePricingStructure() {
  const queryClient = useQueryClient()
  const { close } = useActivateDialog()

  return useMutation({
    mutationFn: async (id: number) => {
      const result = await activatePricingStructureAction(id)
      if (!result.success) throw result
    },
    onSuccess: (_data, id) => {
      invalidateStructure(queryClient, id)
      close()
      toast.success('Pricing structure activated successfully')
    },
    onError: (error: ActionFailure) => {
      handleErrorToast(error, RESOURCE, 'activate')
    },
  })
}

export function useDeactivatePricingStructure() {
  const queryClient = useQueryClient()
  const { close } = useDeactivateDialog()

  return useMutation({
    mutationFn: async (id: number) => {
      const result = await deactivatePricingStructureAction(id)
      if (!result.success) throw result
    },
    onSuccess: (_data, id) => {
      invalidateStructure(queryClient, id)
      close()
      toast.success('Pricing structure deactivated successfully')
    },
    onError: (error: ActionFailure) => {
      handleErrorToast(error, RESOURCE, 'deactivate')
    },
  })
}

export function useDeletePricingStructure() {
  const queryClient = useQueryClient()
  const { close } = useDeleteDialog()

  return useMutation({
    mutationFn: async (id: number) => {
      const result = await deletePricingStructureAction(id)
      if (!result.success) throw result
    },
    onSuccess: (_data, id) => {
      invalidateLists(queryClient)
      queryClient.removeQueries({ queryKey: pricingStructureKeys.detail(id) })
      queryClient.removeQueries({ queryKey: pricingStructureKeys.items(id) })
      close()
      toast.success('Pricing structure deleted successfully')
    },
    onError: (error: ActionFailure) => {
      handleErrorToast(error, RESOURCE, 'delete')
    },
  })
}

export function useUpdatePricingStructureItems(id: number) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (items: PricingStructureItemUpdate[]) => {
      const result = await updatePricingStructureItemsAction(id, items)
      if (!result.success) throw result
      return result.data
    },
    onSuccess: (structure, sent) => {
      // Patch the grid with what was just saved so clearing the drafts doesn't flash the old
      // prices until the refetch lands; the refetch then confirms the server's values.
      const byProduct = new Map(sent.map((s) => [s.productId, s]))
      queryClient.setQueryData<PricingStructureItemRow[]>(pricingStructureKeys.items(id), (rows) =>
        rows?.map((r) => {
          const s = byProduct.get(r.productId)
          return s
            ? { ...r, dealerPackPrice: s.dealerPackPrice, dealerCasePrice: s.dealerCasePrice, mrp: s.mrp }
            : r
        })
      )
      queryClient.invalidateQueries({ queryKey: pricingStructureKeys.items(id) })
      // Seed the detail from the response (pricedCount, updatedAt) instead of refetching it.
      queryClient.setQueryData(pricingStructureKeys.detail(id), structure)
      invalidateLists(queryClient)
      if (structure.isDefault)
        queryClient.invalidateQueries({ queryKey: pricingStructureKeys.defaultPrices() })
      toast.success('Prices saved successfully')
    },
    onError: (error: ActionFailure) => {
      handleErrorToast(error, RESOURCE, 'save prices for')
    },
  })
}
