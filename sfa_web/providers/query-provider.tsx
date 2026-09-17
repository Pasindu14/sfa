"use client";

import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { useState } from "react";
import { Toaster } from 'sonner'
import { queryRetry } from '@/lib/api/query-retry'

export function QueryProvider({ children }: { children: React.ReactNode }) {
  const [queryClient] = useState(
    () =>
      new QueryClient({
        defaultOptions: {
          queries: {
            staleTime: 30 * 1000,
            gcTime: 10 * 60 * 1000,
            // At most 2 retries, and only for 5xx / network / unknown failures — a 4xx
            // (validation, unauthorized, forbidden, not found) fails immediately.
            // Mutations keep their own (unchanged) retry defaults.
            retry: queryRetry,
            refetchOnWindowFocus: false,
            networkMode: 'always',
          },
          mutations: {
            networkMode: 'always',
          },
        },
      })
  );

  return (
    <QueryClientProvider client={queryClient}>
     <Toaster position="top-center" expand />
      {children}
    </QueryClientProvider>
  );
}
