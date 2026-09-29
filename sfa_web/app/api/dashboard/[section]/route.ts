import { NextResponse } from "next/server";
import client, { ApiError } from "@/lib/api/client";
import { getSession } from "@/lib/auth/session";

/**
 * GET /api/dashboard/{sales|activity|trend|breakdown}?date=YYYY-MM-DD
 *
 * A read-only proxy to the API's dashboard sections. This is a Route Handler rather than a server
 * action on purpose: Next.js dispatches server actions from the client ONE AT A TIME, so four
 * section queries built on actions would load in series. Plain GETs run in parallel, which lets
 * each dashboard section render as soon as its own data lands.
 *
 * The proxy matcher skips /api, so authorization is enforced here.
 */
const SECTIONS = new Set(["sales", "activity", "trend", "breakdown"]);
const DATE = /^\d{4}-\d{2}-\d{2}$/;

export async function GET(
  request: Request,
  { params }: { params: Promise<{ section: string }> },
) {
  const { section } = await params;
  if (!SECTIONS.has(section)) {
    return NextResponse.json({ code: "NOT_FOUND", message: "Unknown dashboard section." }, { status: 404 });
  }

  const session = await getSession();
  if (!session?.user) {
    return NextResponse.json({ code: "UNAUTHORIZED", message: "Please sign in." }, { status: 401 });
  }
  if (session.user.role !== "Admin") {
    return NextResponse.json({ code: "FORBIDDEN", message: "The dashboard is available to admins only." }, { status: 403 });
  }

  const date = new URL(request.url).searchParams.get("date");
  if (date !== null && !DATE.test(date)) {
    return NextResponse.json({ code: "VALIDATION_ERROR", message: "date must be YYYY-MM-DD." }, { status: 400 });
  }

  try {
    const res = await client.get(`/api/v1/dashboard/${section}`, { params: date ? { date } : {} });
    return NextResponse.json(res.data.data, { headers: { "Cache-Control": "no-store" } });
  } catch (err) {
    if (err instanceof ApiError) {
      return NextResponse.json({ code: err.code, message: err.message }, { status: err.status || 502 });
    }
    return NextResponse.json({ code: "UPSTREAM_ERROR", message: "Could not load the dashboard." }, { status: 502 });
  }
}
