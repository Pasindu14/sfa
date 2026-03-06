import { Providers } from "@/providers";
import "./globals.css";
import type { Metadata } from "next";
import { Jost } from "next/font/google";

export const metadata: Metadata = {
  title: "Bitlabs Enterprise",
  description: "Bitlabs Enterprise",
};

const jost = Jost({
  variable: "--font-sans",
  subsets: ["latin"],
  weight: ["300", "400", "500", "600", "700"],
});

export default function RootLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <html  lang="en" suppressHydrationWarning>
      <body  className={`${jost.className} antialiased`} >
        <Providers>{children}</Providers>
      </body>
    </html>
  );
}
