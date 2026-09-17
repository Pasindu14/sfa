"use client"

import { Bar, BarChart, CartesianGrid, XAxis, YAxis, ResponsiveContainer, Tooltip } from "recharts"
import type { useMyBillingWeeklyTrend } from "@/features/distributor-billings/hooks/distributor-billing.hooks"

export type WeeklyTrendData = NonNullable<ReturnType<typeof useMyBillingWeeklyTrend>["data"]>

interface WeeklyTrendChartProps {
  data: WeeklyTrendData
  formatCurrency: (amount: number) => string
  formatCurrencyFull: (amount: number) => string
}

/**
 * The recharts bar chart for the dashboard's weekly billing trend. Lives in its own module so
 * the page can load recharts on demand (next/dynamic) instead of in its initial bundle.
 */
export function WeeklyTrendChart({ data, formatCurrency, formatCurrencyFull }: WeeklyTrendChartProps) {
  return (
    <ResponsiveContainer width="100%" height="100%">
      <BarChart data={data} barCategoryGap="28%" barGap={2}>
        <CartesianGrid vertical={false} strokeDasharray="3 3" stroke="hsl(var(--border))" strokeOpacity={0.6} />
        <XAxis dataKey="day" tick={{ fontSize: 10, fill: "hsl(var(--muted-foreground))" }} axisLine={false} tickLine={false} />
        <YAxis tickFormatter={v => formatCurrency(v)} tick={{ fontSize: 9, fill: "hsl(var(--muted-foreground))" }} axisLine={false} tickLine={false} width={58} />
        <Tooltip
          cursor={{ fill: "hsl(var(--muted))", opacity: 0.5 }}
          content={({ active, payload, label }) => {
            if (!active || !payload?.length) return null
            const entry = data?.find(d => d.day === label)
            return (
              <div className="rounded-lg border bg-popover px-3 py-2 text-xs shadow-lg">
                <p className="font-semibold mb-1.5 text-foreground">{entry?.date ?? label}</p>
                {payload.map((p) => (
                  <div key={p.dataKey} className="flex items-center gap-2 justify-between mt-1">
                    <span className="flex items-center gap-1.5 text-muted-foreground capitalize">
                      <span className="h-1.5 w-1.5 rounded-full" style={{ backgroundColor: p.color }} />
                      {p.name}
                    </span>
                    <span className="font-mono font-medium ml-6 text-foreground">{formatCurrencyFull(p.value as number)}</span>
                  </div>
                ))}
              </div>
            )
          }}
        />
        <Bar dataKey="approved" name="Approved" fill="#10b981" radius={[3, 3, 0, 0]} />
        <Bar dataKey="pending" name="Pending" fill="#fbbf24" radius={[3, 3, 0, 0]} />
      </BarChart>
    </ResponsiveContainer>
  )
}
