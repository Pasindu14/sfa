import { Providers } from "@/providers";
import "./globals.css";
import type { Metadata } from "next";
import { Jost, IBM_Plex_Sans } from "next/font/google";
import { cn } from "@/lib/utils";
import NextTopLoader from "nextjs-toploader";

export const metadata: Metadata = {
  title: "Bitlabs Enterprise",
  description: "Bitlabs Enterprise",
};

const jost = Jost({ subsets: ['latin'], variable: '--font-sans' });

/**
 * Report surface only — not applied to the app chrome.
 *
 * Jost is a geometric sans: wide circular figures and a 0 that is hard to tell from an O. That is
 * fine for headings and buttons and poor for a fifteen-column numeric table, where columns have to
 * align and digits have to be unambiguous at 13px. Plex has true tabular lining figures and a
 * narrower fit, so it earns its place on the data surface and nowhere else.
 */
const plex = IBM_Plex_Sans({
  subsets: ['latin'],
  weight: ['400', '500', '600', '700'],
  variable: '--font-report',
});

export default function RootLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <html
      lang="en"
      suppressHydrationWarning
      className={cn("font-sans", jost.variable, plex.variable)}
    >
      <body className={`${jost.className} antialiased overflow-x-hidden`}>
        <NextTopLoader color="#f97316" />
        <Providers>{children}</Providers>
      </body>
    </html>
  );
}
