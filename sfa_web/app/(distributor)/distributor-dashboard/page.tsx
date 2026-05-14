"use client";

import { useSession } from "next-auth/react";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { ShoppingCart, Package, FileText, BarChart3 } from "lucide-react";

const quickLinks = [
  {
    title: "Purchase Orders",
    description: "View and track your purchase orders",
    icon: ShoppingCart,
    href: "/distributor-dashboard/purchase-orders",
  },
  {
    title: "Stock",
    description: "Check your current inventory levels",
    icon: Package,
    href: "/distributor-dashboard/stock",
  },
  {
    title: "GRNs",
    description: "Goods received notes and history",
    icon: FileText,
    href: "/distributor-dashboard/grns",
  },
  {
    title: "Sales Invoices",
    description: "Your invoices and payment records",
    icon: BarChart3,
    href: "/distributor-dashboard/sales-invoices",
  },
];

export default function DistributorDashboardPage() {
  const { data: session } = useSession();
  const name = session?.user?.name;

  return (
    <div className="flex flex-1 flex-col gap-6 p-4 pt-4">
      {/* Welcome */}
      <div className="space-y-1">
        <h1 className="text-2xl font-semibold tracking-tight">
          Welcome back{name ? `, ${name}` : ""}
        </h1>
        <p className="text-sm text-muted-foreground">
          Here&apos;s an overview of your distributor portal.
        </p>
      </div>

      {/* Quick access grid */}
      <div>
        <p className="text-xs font-medium uppercase tracking-widest text-muted-foreground mb-3">
          Quick Access
        </p>
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-4">
          {quickLinks.map((link) => (
            <Card
              key={link.title}
              className="group cursor-pointer transition-colors hover:bg-accent"
            >
              <CardHeader className="pb-2">
                <div className="flex items-center gap-3">
                  <div className="flex h-9 w-9 items-center justify-center rounded-lg bg-primary/10">
                    <link.icon className="h-4 w-4 text-primary" />
                  </div>
                  <CardTitle className="text-base">{link.title}</CardTitle>
                </div>
              </CardHeader>
              <CardContent>
                <p className="text-sm text-muted-foreground">
                  {link.description}
                </p>
              </CardContent>
            </Card>
          ))}
        </div>
      </div>
    </div>
  );
}
