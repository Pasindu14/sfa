import { DefaultSession } from "next-auth";

declare module "next-auth" {
  interface User {
    id: string;
    role: string;
    name: string;
    email?: string | null;
    accessToken?: string;
    refreshToken?: string;
    accessTokenExpiry?: number;
  }
  interface Session {
    user: {
      id: string;
      name: string;
      email?: string | null;
      role: string;
      accessToken?: string;
    };
    accessToken?: string;
    accessTokenExpiry?: number;
    error?: "RefreshAccessTokenError";
  }
}

declare module "next-auth/jwt" {
  interface JWT {
    id: string;
    role: string;
    name: string;
    email?: string | null;
    accessToken?: string;
    refreshToken?: string;
    accessTokenExpiry?: number;
    error?: "RefreshAccessTokenError";
  }
}
