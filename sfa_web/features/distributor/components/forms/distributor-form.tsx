'use client'

import { useEffect } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import {
  createDistributorSchema,
  updateDistributorSchema,
  type CreateDistributorInput,
  type UpdateDistributorInput,
} from "../../schema/distributor.schema";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Textarea } from "@/components/ui/textarea";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import {
  Form,
  FormControl,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from "@/components/ui/form";
import { Spinner } from "@/components/ui/spinner";
import { AsyncSelect } from "@/components/async-select";
import { fetchTerritoriesForSelect } from "@/features/territory/actions/territory.actions";
import type { TerritoryDto } from "@/features/territory/schema/territory.schema";
import { fetchFleetsForSelect } from "@/features/fleet/actions/fleet.actions";
import type { FleetDto } from "@/features/fleet/schema/fleet.schema";

// UpdateDistributorInput is a superset of CreateDistributorInput (adds rowVersion).
interface DistributorFormProps {
  mode: "create" | "edit";
  defaultValues?: Partial<UpdateDistributorInput>;
  onSubmit: (data: UpdateDistributorInput) => void;
  isLoading: boolean;
  fieldErrors?: Record<string, string> | null;
}

export function DistributorForm({
  mode,
  defaultValues,
  onSubmit,
  isLoading,
  fieldErrors,
}: DistributorFormProps) {
  const schema = mode === "create" ? createDistributorSchema : updateDistributorSchema;

  const form = useForm<UpdateDistributorInput>({
    resolver: zodResolver(schema as typeof updateDistributorSchema),
    defaultValues: {
      name: "",
      address: "",
      phone: "",
      email: "",
      alias: 0,
      tradeDiscount: 0,
      commission: 0,
      category: "A" as const,
      remark: "",
      vatRegNo: "",
      latitude: undefined,
      longitude: undefined,
      territoryId: undefined,
      fleetId: undefined,
      rowVersion: 0,
      ...defaultValues,
    },
  });

  const { setError } = form;

  useEffect(() => {
    if (fieldErrors) {
      Object.entries(fieldErrors).forEach(([field, message]) => {
        setError(field as keyof UpdateDistributorInput, { message });
      });
    }
  }, [fieldErrors, setError]);

  return (
    <Form {...form}>
      <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
        <div className="grid grid-cols-2 gap-4">
          <FormField
            control={form.control}
            name="name"
            render={({ field }) => (
              <FormItem>
                <FormLabel>Name</FormLabel>
                <FormControl>
                  <Input placeholder="Enter distributor name" {...field} />
                </FormControl>
                <FormMessage />
              </FormItem>
            )}
          />

          <FormField
            control={form.control}
            name="alias"
            render={({ field }) => (
              <FormItem>
                <FormLabel>Alias (Numbers only)</FormLabel>
                <FormControl>
                  <Input
                    type="number"
                    placeholder="Enter alias (numbers only)"
                    {...field}
                    onChange={(e) =>
                      field.onChange(
                        e.target.value ? parseInt(e.target.value) : 0,
                      )
                    }
                  />
                </FormControl>
                <FormMessage />
              </FormItem>
            )}
          />
        </div>

        <FormField
          control={form.control}
          name="address"
          render={({ field }) => (
            <FormItem>
              <FormLabel>Address</FormLabel>
              <FormControl>
                <Textarea placeholder="Enter address" rows={2} {...field} />
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />

        <div className="grid grid-cols-2 gap-4">
          <FormField
            control={form.control}
            name="phone"
            render={({ field }) => (
              <FormItem>
                <FormLabel>Phone</FormLabel>
                <FormControl>
                  <Input placeholder="+1 234 567 8900" {...field} />
                </FormControl>
                <FormMessage />
              </FormItem>
            )}
          />

          <FormField
            control={form.control}
            name="email"
            render={({ field }) => (
              <FormItem>
                <FormLabel>Email</FormLabel>
                <FormControl>
                  <Input
                    type="email"
                    placeholder="email@example.com"
                    {...field}
                  />
                </FormControl>
                <FormMessage />
              </FormItem>
            )}
          />
        </div>

        <div className="grid grid-cols-2 gap-4">
          <FormField
            control={form.control}
            name="tradeDiscount"
            render={({ field }) => (
              <FormItem>
                <FormLabel>Trade Discount (%) *</FormLabel>
                <FormControl>
                  <Input
                    type="number"
                    step="0.01"
                    min="0"
                    max="100"
                    placeholder="0"
                    {...field}
                    onChange={(e) =>
                      field.onChange(
                        e.target.value ? parseFloat(e.target.value) : 0,
                      )
                    }
                  />
                </FormControl>
                <FormMessage />
              </FormItem>
            )}
          />

          <FormField
            control={form.control}
            name="commission"
            render={({ field }) => (
              <FormItem>
                <FormLabel>Commission (%) *</FormLabel>
                <FormControl>
                  <Input
                    type="number"
                    step="0.01"
                    min="0"
                    max="100"
                    placeholder="0"
                    {...field}
                    onChange={(e) =>
                      field.onChange(
                        e.target.value ? parseFloat(e.target.value) : 0,
                      )
                    }
                  />
                </FormControl>
                <FormMessage />
              </FormItem>
            )}
          />
        </div>

        <FormField
          control={form.control}
          name="category"
          render={({ field }) => (
            <FormItem>
              <FormLabel>Category *</FormLabel>
              <Select onValueChange={field.onChange} value={field.value}>
                <FormControl>
                  <SelectTrigger>
                    <SelectValue placeholder="Select category" />
                  </SelectTrigger>
                </FormControl>
                <SelectContent>
                  {(['A', 'B', 'C', 'D'] as const).map((cat) => (
                    <SelectItem key={cat} value={cat}>
                      Category {cat}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
              <FormMessage />
            </FormItem>
          )}
        />

        <FormField
          control={form.control}
          name="territoryId"
          render={({ field }) => (
            <FormItem>
              <FormLabel>Territory</FormLabel>
              <FormControl>
                <AsyncSelect<TerritoryDto>
                  label="Territory"
                  placeholder="Select territory..."
                  fetcher={fetchTerritoriesForSelect}
                  value={field.value ? String(field.value) : ""}
                  onChange={(v) => field.onChange(v ? Number(v) : null)}
                  getOptionValue={(t) => String(t.id)}
                  getDisplayValue={(t) => t.name}
                  renderOption={(t) => (
                    <div>
                      <span className="font-medium">{t.name}</span>
                      <span className="ml-2 text-xs text-muted-foreground">
                        {t.areaName}
                      </span>
                    </div>
                  )}
                  clearable
                  width="100%"
                />
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />

        <FormField
          control={form.control}
          name="fleetId"
          render={({ field }) => (
            <FormItem>
              <FormLabel>Fleet (Optional)</FormLabel>
              <FormControl>
                <AsyncSelect<FleetDto>
                  label="Fleet"
                  placeholder="Select fleet..."
                  fetcher={fetchFleetsForSelect}
                  value={field.value ? String(field.value) : ""}
                  onChange={(v) => field.onChange(v ? Number(v) : undefined)}
                  getOptionValue={(f) => String(f.id)}
                  getDisplayValue={(f) => f.name}
                  renderOption={(f) => (
                    <span className="font-medium">{f.name}</span>
                  )}
                  clearable
                  width="100%"
                />
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />

        <FormField
          control={form.control}
          name="vatRegNo"
          render={({ field }) => (
            <FormItem>
              <FormLabel>VAT Registration Number (Optional)</FormLabel>
              <FormControl>
                <Input
                  placeholder="Enter VAT registration number"
                  {...field}
                  value={field.value || ""}
                />
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />

        <div className="grid grid-cols-2 gap-4">
          <FormField
            control={form.control}
            name="latitude"
            render={({ field }) => (
              <FormItem>
                <FormLabel>Latitude (Optional)</FormLabel>
                <FormControl>
                  <Input
                    type="number"
                    step="0.000001"
                    placeholder="37.7749"
                    {...field}
                    value={field.value ?? ""}
                    onChange={(e) =>
                      field.onChange(
                        e.target.value ? parseFloat(e.target.value) : undefined,
                      )
                    }
                  />
                </FormControl>
                <FormMessage />
              </FormItem>
            )}
          />

          <FormField
            control={form.control}
            name="longitude"
            render={({ field }) => (
              <FormItem>
                <FormLabel>Longitude (Optional)</FormLabel>
                <FormControl>
                  <Input
                    type="number"
                    step="0.000001"
                    placeholder="-122.4194"
                    {...field}
                    value={field.value ?? ""}
                    onChange={(e) =>
                      field.onChange(
                        e.target.value ? parseFloat(e.target.value) : undefined,
                      )
                    }
                  />
                </FormControl>
                <FormMessage />
              </FormItem>
            )}
          />
        </div>

        <FormField
          control={form.control}
          name="remark"
          render={({ field }) => (
            <FormItem>
              <FormLabel>Remark (Optional)</FormLabel>
              <FormControl>
                <Textarea
                  placeholder="Enter any additional notes"
                  rows={3}
                  {...field}
                  value={field.value || ""}
                />
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />

        {/* Hidden concurrency token — edit mode only */}
        {mode === "edit" && (
          <FormField
            control={form.control}
            name="rowVersion"
            render={({ field }) => (
              <FormItem className="hidden">
                <FormControl>
                  <input
                    type="hidden"
                    {...field}
                    onChange={(e) => field.onChange(Number(e.target.value))}
                    value={field.value ?? 0}
                  />
                </FormControl>
                <FormMessage />
              </FormItem>
            )}
          />
        )}

        <Button type="submit" className="w-full" disabled={isLoading}>
          {isLoading ? (
            <Spinner className="mr-2" />
          ) : mode === "create" ? (
            "Create Distributor"
          ) : (
            "Update Distributor"
          )}
        </Button>
      </form>
    </Form>
  );
}
