import { redirect } from "next/navigation";
import { auth } from "@/auth";

/**
 * Root route gateway. There is no public landing page — `/` only routes the
 * visitor to the right place based on their session:
 *   - not signed in → /sign-in
 *   - distributor   → /distributor-dashboard
 *   - everyone else → /dashboard
 *
 * Runs in the Node runtime (server component), so calling auth() — which pulls
 * in the credentials provider's axios/https deps — is safe here.
 */
export default async function RootPage() {
  const session = await auth();

  if (!session?.user) {
    redirect("/sign-in");
  }

  const role = session.user.role?.toLowerCase();
  redirect(role === "distributor" ? "/distributor-dashboard" : "/users");
}
