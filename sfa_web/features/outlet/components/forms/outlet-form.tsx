"use client";

import { useEffect } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import {
  createOutletSchema,
  type CreateOutletInput,
  OUTLET_TYPES,
  OUTLET_CATEGORIES,
  BILLING_PRICE_TYPES,
  PROVINCES,
  DISTRICTS,
} from "../../schema/outlet.schema";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Textarea } from "@/components/ui/textarea";
import {
  Form,
  FormControl,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from "@/components/ui/form";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { Spinner } from "@/components/ui/spinner";
import { useActiveRoutes } from "@/features/route/hooks/route.hooks";

interface OutletFormProps {
  mode: "create" | "edit";
  defaultValues?: Partial<CreateOutletInput>;
  onSubmit: (data: CreateOutletInput) => void;
  isLoading: boolean;
  fieldErrors?: Record<string, string> | null;
}

export function OutletForm({
  mode,
  defaultValues,
  onSubmit,
  isLoading,
  fieldErrors,
}: OutletFormProps) {
  const { data: activeRoutes, isLoading: isLoadingRoutes } = useActiveRoutes();

  const form = useForm<CreateOutletInput>({
    resolver: zodResolver(createOutletSchema),
    defaultValues: {
      name: "",
      address: "",
      tel: "",
      email: "",
      contactPerson: "",
      nicNo: "",
      vatNo: "",
      creditLimit: 0,
      latitude: 0,
      longitude: 0,
      ownerDOB: "",
      remarks: "",
      image: "",
      outletType: undefined,
      outletCategory: undefined,
      billingPriceType: undefined,
      provinceCode: undefined,
      districtCode: undefined,
      routeId: 0,
      ...defaultValues,
    },
  });

  const { setError } = form;

  useEffect(() => {
    if (fieldErrors) {
      Object.entries(fieldErrors).forEach(([field, message]) => {
        setError(field as keyof CreateOutletInput, { message });
      });
    }
  }, [fieldErrors, setError]);

  return (
    <Form {...form}>
      <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
        {/* Row: Name + NIC */}
        <div className="grid grid-cols-2 gap-4">
          <FormField
            control={form.control}
            name="name"
            render={({ field }) => (
              <FormItem>
                <FormLabel>Name</FormLabel>
                <FormControl>
                  <Input placeholder="Enter outlet name" {...field} />
                </FormControl>
                <FormMessage />
              </FormItem>
            )}
          />

          <FormField
            control={form.control}
            name="nicNo"
            render={({ field }) => (
              <FormItem>
                <FormLabel>NIC Number</FormLabel>
                <FormControl>
                  <Input placeholder="Enter NIC number" {...field} />
                </FormControl>
                <FormMessage />
              </FormItem>
            )}
          />
        </div>

        {/* Address */}
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

        {/* Row: Tel + Email */}
        <div className="grid grid-cols-2 gap-4">
          <FormField
            control={form.control}
            name="tel"
            render={({ field }) => (
              <FormItem>
                <FormLabel>Telephone</FormLabel>
                <FormControl>
                  <Input placeholder="+94 11 234 5678" {...field} />
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
                <FormLabel>Email (Optional)</FormLabel>
                <FormControl>
                  <Input
                    type="email"
                    placeholder="email@example.com"
                    {...field}
                    value={field.value ?? ""}
                  />
                </FormControl>
                <FormMessage />
              </FormItem>
            )}
          />
        </div>

        {/* Row: Contact Person + VAT No */}
        <div className="grid grid-cols-2 gap-4">
          <FormField
            control={form.control}
            name="contactPerson"
            render={({ field }) => (
              <FormItem>
                <FormLabel>Contact Person (Optional)</FormLabel>
                <FormControl>
                  <Input
                    placeholder="Contact person name"
                    {...field}
                    value={field.value ?? ""}
                  />
                </FormControl>
                <FormMessage />
              </FormItem>
            )}
          />

          <FormField
            control={form.control}
            name="vatNo"
            render={({ field }) => (
              <FormItem>
                <FormLabel>VAT Number (Optional)</FormLabel>
                <FormControl>
                  <Input
                    placeholder="Enter VAT number"
                    {...field}
                    value={field.value ?? ""}
                  />
                </FormControl>
                <FormMessage />
              </FormItem>
            )}
          />
        </div>

        {/* Row: Credit Limit + Owner DOB */}
        <div className="grid grid-cols-2 gap-4">
          <FormField
            control={form.control}
            name="creditLimit"
            render={({ field }) => (
              <FormItem>
                <FormLabel>Credit Limit</FormLabel>
                <FormControl>
                  <Input
                    type="number"
                    step="0.01"
                    min="0"
                    placeholder="0.00"
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
            name="ownerDOB"
            render={({ field }) => (
              <FormItem>
                <FormLabel>Owner Date of Birth (Optional)</FormLabel>
                <FormControl>
                  <Input type="date" {...field} value={field.value ?? ""} />
                </FormControl>
                <FormMessage />
              </FormItem>
            )}
          />
        </div>

        {/* Row: Latitude + Longitude */}
        <div className="grid grid-cols-2 gap-4">
          <FormField
            control={form.control}
            name="latitude"
            render={({ field }) => (
              <FormItem>
                <FormLabel>Latitude</FormLabel>
                <FormControl>
                  <Input
                    type="number"
                    step="0.000001"
                    placeholder="6.9271"
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
            name="longitude"
            render={({ field }) => (
              <FormItem>
                <FormLabel>Longitude</FormLabel>
                <FormControl>
                  <Input
                    type="number"
                    step="0.000001"
                    placeholder="79.8612"
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

        {/* Row: Outlet Type + Outlet Category */}
        <div className="grid grid-cols-2 gap-4">
          <FormField
            control={form.control}
            name="outletType"
            render={({ field }) => (
              <FormItem>
                <FormLabel>Outlet Type</FormLabel>
                <Select
                  onValueChange={field.onChange}
                  value={field.value ?? ""}
                >
                  <FormControl>
                    <SelectTrigger className="w-full">
                      <SelectValue placeholder="Select outlet type" />
                    </SelectTrigger>
                  </FormControl>
                  <SelectContent>
                    {OUTLET_TYPES.map((type) => (
                      <SelectItem key={type} value={type}>
                        {type}
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
            name="outletCategory"
            render={({ field }) => (
              <FormItem>
                <FormLabel>Outlet Category</FormLabel>
                <Select
                  onValueChange={field.onChange}
                  value={field.value ?? ""}
                >
                  <FormControl>
                    <SelectTrigger className="w-full">
                      <SelectValue placeholder="Select category" />
                    </SelectTrigger>
                  </FormControl>
                  <SelectContent>
                    {OUTLET_CATEGORIES.map((cat) => (
                      <SelectItem key={cat} value={cat}>
                        {cat}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
                <FormMessage />
              </FormItem>
            )}
          />
        </div>

        {/* Billing Price Type */}
        <FormField
          control={form.control}
          name="billingPriceType"
          render={({ field }) => (
            <FormItem>
              <FormLabel>Billing Price Type (Optional)</FormLabel>
              <Select
                onValueChange={(value) =>
                  field.onChange(value === "none" ? undefined : value)
                }
                value={field.value ?? "none"}
              >
                <FormControl>
                  <SelectTrigger className="w-full">
                    <SelectValue placeholder="Select billing price type" />
                  </SelectTrigger>
                </FormControl>
                <SelectContent>
                  <SelectItem value="none">None</SelectItem>
                  {BILLING_PRICE_TYPES.map((type) => (
                    <SelectItem key={type} value={type}>
                      {type}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
              <FormMessage />
            </FormItem>
          )}
        />

        {/* Row: Province + District */}
        <div className="grid grid-cols-2 gap-4">
          <FormField
            control={form.control}
            name="provinceCode"
            render={({ field }) => (
              <FormItem>
                <FormLabel>Province (Optional)</FormLabel>
                <Select
                  onValueChange={(value) =>
                    field.onChange(value === "none" ? undefined : Number(value))
                  }
                  value={field.value != null ? String(field.value) : "none"}
                >
                  <FormControl>
                    <SelectTrigger className="w-full">
                      <SelectValue placeholder="Select province" />
                    </SelectTrigger>
                  </FormControl>
                  <SelectContent>
                    <SelectItem value="none">None</SelectItem>
                    {PROVINCES.map((p) => (
                      <SelectItem key={p.code} value={String(p.code)}>
                        {p.name}
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
            name="districtCode"
            render={({ field }) => (
              <FormItem>
                <FormLabel>District (Optional)</FormLabel>
                <Select
                  onValueChange={(value) =>
                    field.onChange(value === "none" ? undefined : Number(value))
                  }
                  value={field.value != null ? String(field.value) : "none"}
                >
                  <FormControl>
                    <SelectTrigger className="w-full">
                      <SelectValue placeholder="Select district" />
                    </SelectTrigger>
                  </FormControl>
                  <SelectContent>
                    <SelectItem value="none">None</SelectItem>
                    {DISTRICTS.map((d) => (
                      <SelectItem key={d.code} value={String(d.code)}>
                        {d.name}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
                <FormMessage />
              </FormItem>
            )}
          />
        </div>

        {/* Route */}
        <FormField
          control={form.control}
          name="routeId"
          render={({ field }) => (
            <FormItem>
              <FormLabel>Route</FormLabel>
              <Select
                onValueChange={(value) => field.onChange(Number(value))}
                value={field.value ? String(field.value) : ""}
                disabled={isLoadingRoutes}
              >
                <FormControl>
                  <SelectTrigger className="w-full">
                    <SelectValue
                      placeholder={
                        isLoadingRoutes ? "Loading routes..." : "Select a route"
                      }
                    />
                  </SelectTrigger>
                </FormControl>
                <SelectContent>
                  {(activeRoutes ?? []).map((route) => (
                    <SelectItem key={route.id} value={String(route.id)}>
                      {route.name}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
              <FormMessage />
            </FormItem>
          )}
        />

        {/* Remarks */}
        <FormField
          control={form.control}
          name="remarks"
          render={({ field }) => (
            <FormItem>
              <FormLabel>Remarks (Optional)</FormLabel>
              <FormControl>
                <Textarea
                  placeholder="Enter any additional notes"
                  rows={3}
                  {...field}
                  value={field.value ?? ""}
                />
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />

        <Button type="submit" className="w-full" disabled={isLoading}>
          {isLoading ? (
            <Spinner className="mr-2" />
          ) : mode === "create" ? (
            "Create Outlet"
          ) : (
            "Update Outlet"
          )}
        </Button>
      </form>
    </Form>
  );
}
