import { z } from 'zod'

// Mirrors sfa_api Features/Dashboard/DTOs/DashboardDtos.cs. The dashboard is four independently
// loaded sections. Dates are Colombo `YYYY-MM-DD` business dates; money is LKR. A null
// target/percentage means "no target imported" — render a dash, never 0%.

export const DASHBOARD_SECTIONS = ['sales', 'activity', 'trend', 'breakdown'] as const
export type DashboardSection = (typeof DASHBOARD_SECTIONS)[number]

const salesBlockSchema = z.object({
  targetValue: z.number().nullable(),
  revenue: z.number(),
  achievementPercent: z.number().nullable(),
  grossSaleValue: z.number(),
  discount: z.number(),
  dbDiscount: z.number(),
  totalDiscount: z.number(),
  goodReturn: z.number(),
  marketReturn: z.number(),
  totalReturn: z.number(),
  billCount: z.number(),
})

export const dashboardSalesSchema = z.object({
  date: z.string(),
  monthStart: z.string(),
  monthEnd: z.string(),
  daysInMonth: z.number(),
  daysElapsed: z.number(),
  today: salesBlockSchema,
  monthToDate: salesBlockSchema,
  monthTarget: z.object({
    targetValue: z.number().nullable(),
    expectedToDate: z.number().nullable(),
    achievementPercent: z.number().nullable(),
    balance: z.number().nullable(),
    requiredDailyRate: z.number().nullable(),
  }),
  regions: z.array(
    z.object({
      regionId: z.number().nullable(),
      regionName: z.string(),
      monthTarget: z.number().nullable(),
      revenue: z.number(),
      achievementPercent: z.number().nullable(),
    }),
  ),
  generatedAtUtc: z.string(),
})

export const dashboardActivitySchema = z.object({
  date: z.string(),
  reps: z.object({
    activeToday: z.number(),
    totalReps: z.number(),
    activePercent: z.number().nullable(),
  }),
  outlets: z.object({
    activeOutlets: z.number(),
    inactiveOutlets: z.number(),
    totalCustomers: z.number(),
    activePercent: z.number().nullable(),
    inactivePercent: z.number().nullable(),
    billedLast45Days: z.number(),
    billedLast45DaysPercent: z.number().nullable(),
    billedWindowFrom: z.string(),
    newToday: z.number(),
  }),
  generatedAtUtc: z.string(),
})

const trendPointSchema = z.object({
  date: z.string(),
  revenue: z.number(),
  cumulativeRevenue: z.number(),
})

export const dashboardTrendSchema = z.object({
  date: z.string(),
  monthStart: z.string(),
  daysInMonth: z.number(),
  points: z.array(trendPointSchema),
  generatedAtUtc: z.string(),
})

const rankedSchema = z.object({
  id: z.number().nullable(),
  code: z.string(),
  name: z.string(),
  revenue: z.number(),
  sharePercent: z.number().nullable(),
  quantity: z.number(),
  targetValue: z.number().nullable(),
  achievementPercent: z.number().nullable(),
})

export const NO_SALE_REASONS = ['OutletClosed', 'OwnerAbsent', 'CreditIssue', 'NoOrder', 'OutOfStock'] as const

export const dashboardBreakdownSchema = z.object({
  date: z.string(),
  monthStart: z.string(),
  products: z.array(rankedSchema),
  reps: z.array(rankedSchema),
  distributors: z.array(rankedSchema),
  otherDistributors: z
    .object({ count: z.number(), revenue: z.number(), sharePercent: z.number().nullable() })
    .nullable(),
  visits: z.object({
    saleVisits: z.number(),
    noSaleVisits: z.number(),
    salePercent: z.number().nullable(),
    reasons: z.array(
      z.object({
        // Mirrors the API's NotBillingReason; an unknown future member still parses.
        reason: z.string(),
        count: z.number(),
        sharePercent: z.number().nullable(),
      }),
    ),
  }),
  generatedAtUtc: z.string(),
})

export type DashboardSales = z.infer<typeof dashboardSalesSchema>
export type DashboardActivity = z.infer<typeof dashboardActivitySchema>
export type DashboardTrend = z.infer<typeof dashboardTrendSchema>
export type DashboardSalesBlock = z.infer<typeof salesBlockSchema>
export type DashboardBreakdown = z.infer<typeof dashboardBreakdownSchema>
export type DashboardRanked = z.infer<typeof rankedSchema>