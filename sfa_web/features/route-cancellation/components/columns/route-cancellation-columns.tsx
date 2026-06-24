"use client";

import type { ColumnDef } from "@tanstack/react-table";
import { CheckCircle2, XCircle } from "lucide-react";
import { Button } from "@/components/ui/button";
import {
  Tooltip,
  TooltipContent,
  TooltipProvider,
  TooltipTrigger,
} from "@/components/ui/tooltip";
import { RouteCancellationStatusBadge } from "../badges/route-cancellation-status-badge";
import { useApproveDialog, useRejectDialog } from "../../store";
import {
  DeletionStatus,
  type RouteCancellationDto,
} from "../../schema/route-cancellation.schema";
import { formatColombo } from "@/lib/utils/datetime";

const MONTHS = [
  "Jan",
  "Feb",
  "Mar",
  "Apr",
  "May",
  "Jun",
  "Jul",
  "Aug",
  "Sep",
  "Oct",
  "Nov",
  "Dec",
];

function formatDate(dateStr: string) {
  if (!dateStr) return "—";
  const [year, month, day] = dateStr.split("T")[0].split("-");
  const monthName = MONTHS[parseInt(month, 10) - 1];
  if (!monthName) return "—";
  return `${monthName} ${parseInt(day, 10)}, ${year}`;
}

function formatRelativeTime(dateStr: string | null): string {
  if (!dateStr) return "—";
  const diffMs = Date.now() - new Date(dateStr).getTime();
  const diffHours = Math.floor(diffMs / 3_600_000);
  const diffDays = Math.floor(diffMs / 86_400_000);
  if (diffHours < 1) return "Just now";
  if (diffHours < 24) return `${diffHours}h ago`;
  if (diffDays === 1) return "Yesterday";
  if (diffDays < 7) return `${diffDays}d ago`;
  return formatColombo(dateStr, "d MMM");
}

function TruncatedReason({ reason }: { reason: string | null }) {
  if (!reason) return <span className="text-muted-foreground text-sm">—</span>;
  const truncated = reason.length > 60 ? reason.slice(0, 60) + "…" : reason;
  if (reason.length <= 60) {
    return <span className="text-sm">{reason}</span>;
  }
  return (
    <TooltipProvider>
      <Tooltip>
        <TooltipTrigger asChild>
          <span className="text-sm cursor-default">{truncated}</span>
        </TooltipTrigger>
        <TooltipContent className="max-w-xs text-xs">{reason}</TooltipContent>
      </Tooltip>
    </TooltipProvider>
  );
}

function ActionsCell({ row }: { row: { original: RouteCancellationDto } }) {
  const item = row.original;
  const approveDialog = useApproveDialog();
  const rejectDialog = useRejectDialog();
  const isPending = item.deletionStatus === DeletionStatus.PendingApproval;

  if (!isPending) return null;

  return (
    <div className="flex items-center gap-1">
      <TooltipProvider>
        <Tooltip>
          <TooltipTrigger asChild>
            <Button
              variant="ghost"
              size="icon"
              className="h-8 w-8 text-green-600 hover:text-green-700 hover:bg-green-50"
              onClick={() => approveDialog.open(item.id, item.userName)}
            >
              <CheckCircle2 className="h-4 w-4" />
              <span className="sr-only">Approve</span>
            </Button>
          </TooltipTrigger>
          <TooltipContent>Approve cancellation</TooltipContent>
        </Tooltip>
      </TooltipProvider>

      <TooltipProvider>
        <Tooltip>
          <TooltipTrigger asChild>
            <Button
              variant="ghost"
              size="icon"
              className="h-8 w-8 text-red-600 hover:text-red-700 hover:bg-red-50"
              onClick={() => rejectDialog.open(item.id, item.userName)}
            >
              <XCircle className="h-4 w-4" />
              <span className="sr-only">Reject</span>
            </Button>
          </TooltipTrigger>
          <TooltipContent>Reject cancellation</TooltipContent>
        </Tooltip>
      </TooltipProvider>
    </div>
  );
}

export function getRouteCancellationColumns(): ColumnDef<RouteCancellationDto>[] {
  return [
    {
      accessorKey: "date",
      header: "Assignment Date",
      cell: ({ row }) => (
        <span className="text-sm font-medium">
          {formatDate(row.original.date)}
        </span>
      ),
    },
    {
      accessorKey: "userName",
      header: "Sales Rep",
      cell: ({ row }) => (
        <span className="font-medium">{row.original.userName}</span>
      ),
    },
    {
      accessorKey: "routeCode",
      header: "Route",
      cell: ({ row }) => (
        <div className="flex flex-col">
          <span className="font-mono text-xs font-semibold text-primary">
            {row.original.routeCode}
          </span>
          <span className="text-xs text-muted-foreground">
            {row.original.routeName}
          </span>
        </div>
      ),
    },
    {
      accessorKey: "deletionRequestReason",
      header: "Cancellation Reason",
      cell: ({ row }) => (
        <TruncatedReason reason={row.original.deletionRequestReason} />
      ),
    },
    {
      accessorKey: "deletionRequestedAt",
      header: "Requested",
      cell: ({ row }) => (
        <TooltipProvider>
          <Tooltip>
            <TooltipTrigger asChild>
              <span className="text-sm text-muted-foreground cursor-default">
                {formatRelativeTime(row.original.deletionRequestedAt)}
              </span>
            </TooltipTrigger>
            <TooltipContent>
              {formatColombo(row.original.deletionRequestedAt, "d MMM yyyy, HH:mm")}
            </TooltipContent>
          </Tooltip>
        </TooltipProvider>
      ),
    },
    {
      accessorKey: "deletionStatus",
      header: "Status",
      cell: ({ row }) => (
        <RouteCancellationStatusBadge status={row.original.deletionStatus} />
      ),
    },
    {
      id: "actions",
      header: () => <div className="text-right pr-2">Actions</div>,
      cell: ({ row }) => (
        <div className="flex justify-end">
          <ActionsCell row={row} />
        </div>
      ),
    },
  ];
}
