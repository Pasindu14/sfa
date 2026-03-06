"use client";

import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { useState } from "react";
import { Toaster } from 'sonner'

export function QueryProvider({ children }: { children: React.ReactNode }) {
  const [queryClient] = useState(
    () =>
      new QueryClient({
        defaultOptions: {
          queries: {
            staleTime: 60 * 1000,
            networkMode: 'always',
            retry: 3,
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
