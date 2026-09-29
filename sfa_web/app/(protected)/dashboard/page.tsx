"use client";

import dynamic from "next/dynamic";
import { useSession } from "next-auth/react";
import { ErrorBoundary } from "@/components/error-boundary";
import { ErrorState } from "@/components/error-state";

const DashboardPage = dynamic(
  () =>
    import("@/features/dashboard/components").then((m) => ({
      default: m.DashboardPage,
    })),
  { ssr: false },
);

export default function DashboardRoutePage() {
  const { data: session, status } = useSession();

  if (status === "loading") return null;

  // The dashboard API is Admin-only. Other back-office roles land here after sign-in, so greet
  // them instead of showing a failed request.
  if (session?.user?.role !== "Admin") {
    return (
      <div className="flex flex-1 flex-col gap-2 p-6 pt-0">
        <h1 className="text-2xl font-semibold tracking-tight">
          Welcome{session?.user?.name ? `, ${session.user.name}` : ""}
        </h1>
        <p className="text-sm text-muted-foreground">
          Use the menu to get started.
        </p>
      </div>
    );
  }

  return (
    <ErrorBoundary fallback={<ErrorState />}>
      <DashboardPage />
    </ErrorBoundary>
  );
}
