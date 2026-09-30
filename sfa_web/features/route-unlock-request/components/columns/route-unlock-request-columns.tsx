"use client";

import type { ColumnDef } from "@tanstack/react-table";
import { Ban, CheckCircle2, Eye, XCircle } from "lucide-react";
import { Button } from "@/components/ui/button";
import {
  Tooltip,
  TooltipContent,
  TooltipProvider,
  TooltipTrigger,
} from "@/components/ui/tooltip";
import { RouteUnlockStatusBadge } from "../badges/route-unlock-status-badge";
import {
  useApproveUnlockDialog,
  useRejectUnlockDialog,
  useRevokeUnlockDialog,
  useUnlockDetailSheet,
} from "../../store";
import {
  RouteUnlockStatus,
  type RouteUnlockRequestDto,
} from "../../schema/route-unlock-request.schema";
import {
  formatBusinessDate,
  formatRelativeTime,
  formatTimestamp,
} from "../format";

function TruncatedReason({ reason }: { reason: string | null }) {
  if (!reason) return <span className="text-muted-foreground text-sm">—</span>;
  if (reason.length <= 60) {
    return <span className="text-sm">{reason}</span>;
  }
  return (
    <TooltipProvider>
      <Tooltip>
        <TooltipTrigger asChild>
          <span className="text-sm cursor-default">{reason.slice(0, 60) + "…"}</span>
        </TooltipTrigger>
        <TooltipContent className="max-w-xs text-xs">{reason}</TooltipContent>
      </Tooltip>
    </TooltipProvider>
  );
}

function IconAction({
  label,
  className,
  onClick,
  children,
}: {
  label: string;
  className?: string;
  onClick: () => void;
  children: React.ReactNode;
}) {
  return (
    <TooltipProvider>
      <Tooltip>
        <TooltipTrigger asChild>
          <Button variant="ghost" size="icon" className={`h-8 w-8 ${className ?? ""}`} onClick={onClick}>
            {children}
            <span className="sr-only">{label}</span>
          </Button>
        </TooltipTrigger>
        <TooltipContent>{label}</TooltipContent>
      </Tooltip>
    </TooltipProvider>
  );
}

function ActionsCell({ row }: { row: { original: RouteUnlockRequestDto } }) {
  const item = row.original;
  const approveDialog = useApproveUnlockDialog();
  const rejectDialog = useRejectUnlockDialog();
  const revokeDialog = useRevokeUnlockDialog();
  const detailSheet = useUnlockDetailSheet();

  const isPending = item.effectiveStatus === RouteUnlockStatus.Pending;
  const selected = {
    id: item.id,
    rowVersion: item.rowVersion,
    repName: item.userName,
    routeName: item.routeName,
  };

  return (
    <div className="flex items-center gap-1">
      <IconAction label="View details" onClick={() => detailSheet.open(item.id)}>
        <Eye className="h-4 w-4" />
      </IconAction>

      {isPending && (
        <>
          <IconAction
            label="Approve unlock"
            className="text-green-600 hover:text-green-700 hover:bg-green-50"
            onClick={() => approveDialog.open(selected)}
          >
            <CheckCircle2 className="h-4 w-4" />
          </IconAction>
          <IconAction
            label="Reject unlock"
            className="text-red-600 hover:text-red-700 hover:bg-red-50"
            onClick={() => rejectDialog.open(selected)}
          >
            <XCircle className="h-4 w-4" />
          </IconAction>
        </>
      )}

      {item.isCurrentlyEffective && (
        <IconAction
          label="Revoke unlock"
          className="text-orange-600 hover:text-orange-700 hover:bg-orange-50"
          onClick={() => revokeDialog.open(selected)}
        >
          <Ban className="h-4 w-4" />
        </IconAction>
      )}
    </div>
  );
}

export function getRouteUnlockRequestColumns(): ColumnDef<RouteUnlockRequestDto>[] {
  return [
    {
      accessorKey: "userName",
      header: "Rep",
      cell: ({ row }) => (
        <div className="flex flex-col">
          <span className="font-medium">{row.original.userName}</span>
          <span className="text-xs text-muted-foreground">{row.original.loginName}</span>
        </div>
      ),
    },
    {
      accessorKey: "routeName",
      header: "Route",
      cell: ({ row }) => <span className="text-sm">{row.original.routeName}</span>,
    },
    {
      accessorKey: "businessDate",
      header: "Date",
      cell: ({ row }) => (
        <span className="text-sm font-medium whitespace-nowrap">
          {formatBusinessDate(row.original.businessDate)}
        </span>
      ),
    },
    {
      accessorKey: "requestedAt",
      header: "Requested",
      cell: ({ row }) => (
        <TooltipProvider>
          <Tooltip>
            <TooltipTrigger asChild>
              <span className="text-sm text-muted-foreground cursor-default whitespace-nowrap">
                {formatRelativeTime(row.original.requestedAt)}
              </span>
            </TooltipTrigger>
            <TooltipContent>{formatTimestamp(row.original.requestedAt)}</TooltipContent>
          </Tooltip>
        </TooltipProvider>
      ),
    },
    {
      accessorKey: "requestReason",
      header: "Reason",
      cell: ({ row }) => <TruncatedReason reason={row.original.requestReason} />,
    },
    {
      accessorKey: "supervisorName",
      header: "Routed to",
      cell: ({ row }) =>
        row.original.supervisorName ? (
          <span className="text-sm">{row.original.supervisorName}</span>
        ) : (
          <span className="text-sm text-muted-foreground">— (no supervisor)</span>
        ),
    },
    {
      accessorKey: "effectiveStatus",
      header: "Status",
      cell: ({ row }) => <RouteUnlockStatusBadge status={row.original.effectiveStatus} />,
    },
    {
      accessorKey: "reviewedByName",
      header: "Reviewed by",
      cell: ({ row }) => {
        const r = row.original;
        if (!r.reviewedByName) return <span className="text-sm text-muted-foreground">—</span>;
        return (
          <div className="flex flex-col">
            <span className="text-sm">
              {r.reviewedByName}
              {r.reviewedByRole && (
                <span className="text-xs text-muted-foreground"> · {r.reviewedByRole}</span>
              )}
            </span>
            <span className="text-xs text-muted-foreground whitespace-nowrap">
              {formatTimestamp(r.reviewedAt)}
            </span>
          </div>
        );
      },
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
