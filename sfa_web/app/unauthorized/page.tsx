"use client";

import { useSession, signOut } from "next-auth/react";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Separator } from "@/components/ui/separator";
import { ShieldX, LogOut } from "lucide-react";

export default function UnauthorizedPage() {
  const { data: session } = useSession();
  const role = session?.user?.role;

  return (
    <div className="relative min-h-svh flex flex-col items-center justify-center overflow-hidden bg-[#B91C1C] p-12">
      {/* Gradient overlay — matches login panel */}
      <div className="absolute inset-0 bg-gradient-to-br from-red-950 via-red-800 to-red-700" />

      {/* Decorative blobs — matches login panel */}
      <div className="absolute -bottom-24 -left-24 h-72 w-72 rounded-full bg-red-900/50" />
      <div className="absolute -top-24 -right-24 h-96 w-96 rounded-full bg-red-600/20" />
      <div className="absolute bottom-1/3 right-0 h-48 w-48 rounded-full bg-red-900/30" />

      {/* Content */}
      <div className="relative z-10 flex flex-col items-center gap-6 text-center text-white max-w-sm w-full">

        {/* Icon */}
        <div className="flex h-20 w-20 items-center justify-center rounded-full border border-white/20 bg-white/10">
          <ShieldX className="h-9 w-9" strokeWidth={1.5} />
        </div>

        {/* Heading + subtext */}
        <div className="space-y-3">
          <h1 className="text-3xl font-bold tracking-tight">Access Denied</h1>
          <p className="text-red-200 text-sm leading-relaxed">
            Your account does not have permission to access this area.
            Contact your administrator if you believe this is a mistake.
          </p>
        </div>

        {/* Role badge */}
        {role && (
          <div className="flex items-center gap-2">
            <span className="text-red-300 text-xs uppercase tracking-widest">
              Signed in as
            </span>
            <Badge className="bg-white/10 text-white border border-white/20 uppercase text-xs">
              {role}
            </Badge>
          </div>
        )}

        <Separator className="w-16 bg-red-500/50" />

        {/* Sign out */}
        <Button
          variant="outline"
          className="border-white/30 bg-transparent text-white hover:bg-white/10 hover:text-white hover:border-white/50"
          onClick={() => signOut({ callbackUrl: "/sign-in" })}
        >
          <LogOut className="mr-2 h-4 w-4" />
          Sign out
        </Button>

        {/* Footer */}
        <p className="text-red-300 text-xs">
          Uswatte Biscuits &mdash; Internal Portal
        </p>
      </div>
    </div>
  );
}
