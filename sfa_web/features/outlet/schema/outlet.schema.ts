import { z } from 'zod'

// --- Constants for Select fields ---

export const OUTLET_TYPES = ['Small', 'Medium', 'Large'] as const
export const OUTLET_CATEGORIES = ['Wholesale', 'SMMT'] as const
export const BILLING_PRICE_TYPES = ['DealerPrice', 'OldPrice', 'MarketPrice'] as const

export const PROVINCES = [
  { code: 1, name: 'Western' },
  { code: 2, name: 'Central' },
  { code: 3, name: 'Southern' },
  { code: 4, name: 'Northern' },
  { code: 5, name: 'Eastern' },
  { code: 6, name: 'North Western' },
  { code: 7, name: 'North Central' },
  { code: 8, name: 'Uva' },
  { code: 9, name: 'Sabaragamuwa' },
] as const

export const DISTRICTS = [
  { code: 1, name: 'Colombo' },
  { code: 2, name: 'Gampaha' },
  { code: 3, name: 'Kalutara' },
  { code: 4, name: 'Kandy' },
  { code: 5, name: 'Matale' },
  { code: 6, name: 'Nuwara Eliya' },
  { code: 7, name: 'Galle' },
  { code: 8, name: 'Matara' },
  { code: 9, name: 'Hambantota' },
  { code: 10, name: 'Jaffna' },
  { code: 11, name: 'Kilinochchi' },
  { code: 12, name: 'Mannar' },
  { code: 13, name: 'Vavuniya' },
  { code: 14, name: 'Mullaitivu' },
  { code: 15, name: 'Batticaloa' },
  { code: 16, name: 'Ampara' },
  { code: 17, name: 'Trincomalee' },
  { code: 18, name: 'Kurunegala' },
  { code: 19, name: 'Puttalam' },
  { code: 20, name: 'Anuradhapura' },
  { code: 21, name: 'Polonnaruwa' },
  { code: 22, name: 'Badulla' },
  { code: 23, name: 'Monaragala' },
  { code: 24, name: 'Ratnapura' },
  { code: 25, name: 'Kegalle' },
] as const

// --- Create schema ---

export const createOutletSchema = z.object({
  name: z.string().min(1, 'Name is required'),
  address: z.string().min(1, 'Address is required'),
  tel: z.string().min(1, 'Telephone is required'),
  email: z.string().email('Invalid email format').optional().or(z.literal('')),
  contactPerson: z.string().optional(),
  nicNo: z.string().min(1, 'NIC number is required'),
  vatNo: z.string().optional(),
  creditLimit: z.number().min(0, 'Credit limit cannot be negative'),
  latitude: z.number(),
  longitude: z.number(),
  ownerDOB: z.string().optional(),
  remarks: z.string().optional(),
  image: z.string().optional(),
  outletType: z.enum(OUTLET_TYPES, { error: 'Outlet type is required' }),
  outletCategory: z.enum(OUTLET_CATEGORIES, { error: 'Outlet category is required' }),
  billingPriceType: z.enum(BILLING_PRICE_TYPES).optional(),
  provinceCode: z.number().int().optional(),
  districtCode: z.number().int().optional(),
  routeId: z.number({ error: 'Route is required' }).int().min(1, 'Route is required'),
})

// --- Update schema (same shape as create) ---

export const updateOutletSchema = createOutletSchema

// --- Filter schema ---

export const filterSchema = z.object({
  search: z.string().optional(),
  status: z.string().optional(),
  page: z.number().default(1),
  pageSize: z.number().default(10),
})

// --- Inferred TypeScript types ---

export type CreateOutletInput = z.infer<typeof createOutletSchema>
export type UpdateOutletInput = z.infer<typeof updateOutletSchema>
export type OutletFilterInput = z.infer<typeof filterSchema>

// --- DTO type (matches API response) ---

export type OutletDto = {
  id: number
  name: string
  address: string
  tel: string
  email: string | null
  contactPerson: string | null
  nicNo: string
  vatNo: string | null
  creditLimit: number
  latitude: number
  longitude: number
  ownerDOB: string | null
  remarks: string | null
  image: string | null
  outletType: string
  outletCategory: string
  billingPriceType: string | null
  provinceCode: number | null
  districtCode: number | null
  routeId: number
  routeName: string
  divisionId: number
  divisionName: string
  territoryId: number
  territoryName: string
  areaId: number
  areaName: string
  regionId: number
  regionName: string
  isActive: boolean
  createdAt: string
  updatedAt: string
}

// --- Slim DTO for map rendering (id, name, lat, lng only) ---

export type OutletMapPointDto = {
  id: number
  name: string
  latitude: number
  longitude: number
}
