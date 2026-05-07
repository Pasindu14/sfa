'use client'

import { useCallback } from 'react'
import { DataTable } from '@/components/data-table/data-table'
import { Button } from '@/components/ui/button'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { Plus } from "lucide-react";
import { useUserDialogStore } from "../../store";
import { useUserDataTable } from "../../hooks/user.hooks";
import { getUserColumns } from "../columns/user-columns";

export function UserTable() {
  const openCreate = useUserDialogStore((s) => s.openCreate);
  const openEdit = useUserDialogStore((s) => s.openEdit);
  const openDelete = useUserDialogStore((s) => s.openDelete);
  const openChangePassword = useUserDialogStore((s) => s.openChangePassword);
  const openActivate = useUserDialogStore((s) => s.openActivate);
  const openDeactivate = useUserDialogStore((s) => s.openDeactivate);

  const getColumns = useCallback(
    () =>
      getUserColumns({
        openEdit,
        openDelete,
        openChangePassword,
        openActivate,
        openDeactivate,
      }),
    [openEdit, openDelete, openChangePassword, openActivate, openDeactivate],
  );

  return (
    <DataTable
      config={{
        enableRowSelection: false,
        enableSearch: true,
        enableDateFilter: false,
        enableExport: false,
        enableColumnResizing: false,
        enableUrlState: false,
        columnResizingTableId: "users-table",
        searchPlaceholder: "Search users...",
      }}
      getColumns={getColumns}
      fetchDataFn={useUserDataTable}
      exportConfig={{
        entityName: "users",
        columnMapping: {
          name: "Name",
          username: "Username",
          email: "Email",
          phone: "Phone",
          role: "Role",
          isActive: "Status",
          createdAt: "Created At",
        },
        columnWidths: [
          { wch: 25 },
          { wch: 15 },
          { wch: 25 },
          { wch: 15 },
          { wch: 12 },
          { wch: 10 },
          { wch: 20 },
        ],
        headers: [
          "Name",
          "Username",
          "Email",
          "Phone",
          "Role",
          "Status",
          "Created At",
        ],
      }}
      idField="id"
      renderCustomFilters={(filters, setFilters) => (
        <Select
          value={(filters?.role as string) ?? ""}
          onValueChange={(value) =>
            setFilters({ ...filters, role: value === "all" ? "" : value })
          }
        >
          <SelectTrigger className="h-8 w-32.5">
            <SelectValue placeholder="All Roles" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">All Roles</SelectItem>
            <SelectItem value="Admin">Admin</SelectItem>
            <SelectItem value="Manager">Manager</SelectItem>
            <SelectItem value="SalesRep">Sales Rep</SelectItem>
          </SelectContent>
        </Select>
      )}
      renderToolbarContent={() => (
        <Button onClick={openCreate} className="gap-2">
          <Plus className="h-4 w-4" />
          Add User
        </Button>
      )}
    />
  );
}
