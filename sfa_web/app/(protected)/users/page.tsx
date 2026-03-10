"use client";
import dynamic from "next/dynamic";

const UserListPage = dynamic(
  () =>
    import("@/features/user/components").then((m) => ({
      default: m.UserListPage,
    })),
  { ssr: false },
);

export default function UsersPage() {
  return <UserListPage />;
}
