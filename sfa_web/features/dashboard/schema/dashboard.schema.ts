import { z } from 'zod'

// Mirrors sfa_api Features/Dashboard/DTOs/DashboardDtos.cs. Dates are Colombo `YYYY-MM-DD`
// business dates; money is LKR. A null target/percentage means "no target imported" — render a
// dash, never 0%.

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

export const dashboardSchema = z.object({
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
  dailyTrend: z.array(
    z.object({
      date: z.string(),
      revenue: z.number(),
      target: z.number().nullable(),
      cumulativeRevenue: z.number(),
      cumulativeTarget: z.number().nullable(),
    }),
  ),
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

export type DashboardDto = z.infer<typeof dashboardSchema>
export type DashboardSalesBlock = z.infer<typeof salesBlockSchema>
export type DashboardDailyPoint = DashboardDto['dailyTrend'][number]
export type DashboardRegionRow = DashboardDto['regions'][number]
