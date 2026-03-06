import { DefaultSession } from "next-auth";

declare module "next-auth" {
  interface User {
    id: string;
    role: string;
    name: string;
    email?: string | null;
  }
  interface Session {
    user: {
      id: string;
      name: string;
      email?: string | null;
      role: string;
    };
  }
}

declare module "next-auth/jwt" {
  interface JWT {
    id: string;
    role: string;
    name: string;
    email?: string | null;
  }
}
