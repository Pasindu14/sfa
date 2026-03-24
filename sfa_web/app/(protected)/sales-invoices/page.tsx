"use client";

import dynamic from "next/dynamic";
import { ErrorBoundary } from "@/components/error-boundary";
import { ErrorState } from "@/components/error-state";

const SalesInvoiceListPage = dynamic(
  () =>
    import("@/features/sales-invoice/components").then((m) => ({
      default: m.SalesInvoiceListPage,
    })),
  { ssr: false },
);

export default function SalesInvoicesPage() {
  return (
    <ErrorBoundary fallback={<ErrorState />}>
      <SalesInvoiceListPage />
    </ErrorBoundary>
  );
}
