"use client";

import { useEffect } from "react";
import { Button } from "@/components/ui/button";
import { AlertCircle } from "lucide-react";

export default function GlobalError({
  error,
  reset,
}: {
  error: Error & { digest?: string };
  reset: () => void;
}) {
  useEffect(() => {
    console.error("GlobalError:", error);
  }, [error]);

  return (
    <html lang="en">
      <body className="antialiased">
        <div className="flex min-h-screen flex-col items-center justify-center gap-4">
          <div className="flex items-center gap-2 text-destructive">
            <AlertCircle className="h-6 w-6" />
            <h2 className="text-lg font-semibold">Something went wrong</h2>
          </div>
          <p className="text-sm text-muted-foreground">
            {error.message || "An unexpected error occurred"}
          </p>
          <Button onClick={reset} variant="outline">
            Try again
          </Button>
        </div>
      </body>
    </html>
  );
}
