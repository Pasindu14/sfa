"use client";
import { ChevronRight, type LucideIcon } from "lucide-react";
import Link from "next/link";
import { usePathname } from "next/navigation";
import { useState, useEffect } from "react";
import {
  Collapsible,
  CollapsibleContent,
  CollapsibleTrigger,
} from "@/components/ui/collapsible";
import {
  SidebarGroup,
  SidebarGroupLabel,
  SidebarMenu,
  SidebarMenuButton,
  SidebarMenuItem,
  SidebarMenuSub,
  SidebarMenuSubButton,
  SidebarMenuSubItem,
} from "@/components/ui/sidebar";

export function NavMain({
  items,
}: {
  items: {
    title: string;
    url: string;
    icon?: LucideIcon;
    isActive?: boolean;
    items?: {
      title: string;
      url: string;
    }[];
  }[];
}) {
  const pathname = usePathname();

  function getActiveTitle() {
    return (
      items.find((item) =>
        item.items?.some((sub) => pathname.startsWith(sub.url))
      )?.title ?? null
    );
  }

  // Manually opened sections (in addition to the always-open active section)
  const [manualOpen, setManualOpen] = useState<Set<string>>(new Set());

  // When route changes, clear manual overrides — active section will show on its own
  useEffect(() => {
    setManualOpen(new Set());
  }, [pathname]);

  const activeTitle = getActiveTitle();

  function isOpen(title: string) {
    return title === activeTitle || manualOpen.has(title);
  }

  function toggle(title: string) {
    if (title === activeTitle) return; // active section is always open, never toggle it
    setManualOpen((prev) => {
      const next = new Set(prev);
      if (next.has(title)) {
        next.delete(title);
      } else {
        next.add(title);
      }
      return next;
    });
  }

  return (
    <SidebarGroup>
      <SidebarGroupLabel>Administration</SidebarGroupLabel>
      <SidebarMenu>
        {items.map((item) => (
          <Collapsible
            key={item.title}
            asChild
            open={isOpen(item.title)}
            onOpenChange={() => toggle(item.title)}
            className="group/collapsible"
          >
            <SidebarMenuItem>
              <CollapsibleTrigger asChild>
                <SidebarMenuButton tooltip={item.title}>
                  {item.icon && <item.icon />}
                  <span>{item.title}</span>
                  <ChevronRight className="ml-auto transition-transform duration-200 group-data-[state=open]/collapsible:rotate-90" />
                </SidebarMenuButton>
              </CollapsibleTrigger>
              <CollapsibleContent>
                <SidebarMenuSub>
                  {item.items?.map((subItem) => (
                    <SidebarMenuSubItem key={subItem.title}>
                      <SidebarMenuSubButton asChild>
                        <Link
                          href={subItem.url}
                          className={
                            pathname === subItem.url
                              ? "bg-primary/10 text-primary font-medium"
                              : ""
                          }
                        >
                          <span>{subItem.title}</span>
                        </Link>
                      </SidebarMenuSubButton>
                    </SidebarMenuSubItem>
                  ))}
                </SidebarMenuSub>
              </CollapsibleContent>
            </SidebarMenuItem>
          </Collapsible>
        ))}
      </SidebarMenu>
    </SidebarGroup>
  );
}
