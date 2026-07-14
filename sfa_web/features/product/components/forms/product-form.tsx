'use client'

import { useEffect } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import {
  createProductSchema,
  updateProductSchema,
  type UpdateProductInput,
  type UpdateProductFormInput,
} from '../../schema/product.schema'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Textarea } from '@/components/ui/textarea'
import {
  Form,
  FormControl,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from '@/components/ui/form'
import { Spinner } from '@/components/ui/spinner'
import { AsyncSelect } from '@/components/async-select'
import { fetchFleetsForSelect } from '@/features/fleet/actions/fleet.actions'
import { fetchProductCategoriesForSelect } from '@/features/product-category/actions/product-category.actions'
import type { FleetDto } from '@/features/fleet/schema/fleet.schema'
import type { ProductCategoryDto } from '@/features/product-category/schema/product-category.schema'

// UpdateProductInput is a superset of CreateProductInput (adds rowVersion).
interface ProductFormProps {
  mode: 'create' | 'edit'
  defaultValues?: Partial<UpdateProductInput>
  onSubmit: (data: UpdateProductInput) => void
  isLoading: boolean
  fieldErrors?: Record<string, string> | null
}

export function ProductForm({
  mode,
  defaultValues,
  onSubmit,
  isLoading,
  fieldErrors,
}: ProductFormProps) {
  const schema = mode === 'create' ? createProductSchema : updateProductSchema

  // Three-generic useForm: fields work in the INPUT type (numeric fields may be
  // transiently empty/undefined while typing), handleSubmit yields the parsed
  // OUTPUT type (UpdateProductInput) with `.default(0)` applied.
  const form = useForm<UpdateProductFormInput, unknown, UpdateProductInput>({
    resolver: zodResolver(schema as typeof updateProductSchema),
    defaultValues: {
      code: "",
      itemDescription: "",
      printDescription: "",
      piecesPerPack: 0,
      imageUrl: "",
      remarks: "",
      fleetId: undefined,
      categoryId: undefined,
      dealerPackPrice: undefined,
      dealerCasePrice: undefined,
      mrp: undefined,
      rowVersion: 0,
      ...defaultValues,
    },
  });

  const { setError, reset } = form;

  useEffect(() => {
    if (defaultValues) {
      reset({
        code: "",
        itemDescription: "",
        printDescription: "",
        piecesPerPack: 0,
        imageUrl: "",
        remarks: "",
        fleetId: undefined,
        categoryId: undefined,
        dealerPackPrice: undefined,
        dealerCasePrice: undefined,
        mrp: undefined,
        rowVersion: 0,
        ...defaultValues,
      });
    }
  }, [defaultValues, reset]);

  useEffect(() => {
    if (fieldErrors) {
      Object.entries(fieldErrors).forEach(([field, message]) => {
        setError(field as keyof UpdateProductFormInput, { message })
      })
    }
  }, [fieldErrors, setError])

  return (
    <Form {...form}>
      <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
        <div className="grid grid-cols-2 gap-4">
          <FormField
            control={form.control}
            name="code"
            render={({ field }) => (
              <FormItem>
                <FormLabel>Code *</FormLabel>
                <FormControl>
                  <Input placeholder="e.g. PROD-001" {...field} />
                </FormControl>
                <FormMessage />
              </FormItem>
            )}
          />

          <FormField
            control={form.control}
            name="piecesPerPack"
            render={({ field }) => (
              <FormItem>
                <FormLabel>Pieces Per Pack *</FormLabel>
                <FormControl>
                  <Input
                    type="number"
                    min="0"
                    placeholder="0"
                    {...field}
                    value={field.value ?? ''}
                    onChange={(e) =>
                      field.onChange(
                        e.target.value === '' ? undefined : parseInt(e.target.value, 10),
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
          name="itemDescription"
          render={({ field }) => (
            <FormItem>
              <FormLabel>Item Description *</FormLabel>
              <FormControl>
                <Input placeholder="Full display name for the product" {...field} />
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />

        <FormField
          control={form.control}
          name="printDescription"
          render={({ field }) => (
            <FormItem>
              <FormLabel>Print Description (Optional)</FormLabel>
              <FormControl>
                <Input
                  placeholder="Uppercase label used on print/reports"
                  {...field}
                  value={field.value ?? ''}
                  // Force uppercase for every input path — typing, paste, drop, autofill.
                  onChange={(e) => field.onChange(e.target.value.toUpperCase())}
                />
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />

        <FormField
          control={form.control}
          name="imageUrl"
          render={({ field }) => (
            <FormItem>
              <FormLabel>Image URL (Optional)</FormLabel>
              <FormControl>
                <Input
                  placeholder="https://example.com/image.png"
                  {...field}
                  value={field.value ?? ''}
                />
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />

        <FormField
          control={form.control}
          name="remarks"
          render={({ field }) => (
            <FormItem>
              <FormLabel>Remarks (Optional)</FormLabel>
              <FormControl>
                <Textarea
                  placeholder="Any additional notes"
                  rows={3}
                  {...field}
                  value={field.value ?? ''}
                />
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />

        <div className="space-y-2">
          <p className="text-sm font-medium text-muted-foreground">Pricing</p>
          <div className="grid grid-cols-3 gap-4">
            <FormField
              control={form.control}
              name="dealerPackPrice"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Dealer Pack Price</FormLabel>
                  <FormControl>
                    <Input
                      type="number"
                      min="0"
                      step="0.01"
                      placeholder="0.00"
                      {...field}
                      value={field.value ?? ''}
                      onChange={(e) =>
                        field.onChange(
                          e.target.value === '' ? undefined : parseFloat(e.target.value),
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
              name="dealerCasePrice"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>Dealer Case Price</FormLabel>
                  <FormControl>
                    <Input
                      type="number"
                      min="0"
                      step="0.01"
                      placeholder="0.00"
                      {...field}
                      value={field.value ?? ''}
                      onChange={(e) =>
                        field.onChange(
                          e.target.value === '' ? undefined : parseFloat(e.target.value),
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
              name="mrp"
              render={({ field }) => (
                <FormItem>
                  <FormLabel>MRP</FormLabel>
                  <FormControl>
                    <Input
                      type="number"
                      min="0"
                      step="0.01"
                      placeholder="0.00"
                      {...field}
                      value={field.value ?? ''}
                      onChange={(e) =>
                        field.onChange(
                          e.target.value === '' ? undefined : parseFloat(e.target.value),
                        )
                      }
                    />
                  </FormControl>
                  <FormMessage />
                </FormItem>
              )}
            />
          </div>
        </div>

        <div className="grid grid-cols-2 gap-4">
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
                    value={field.value ? String(field.value) : ''}
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
            name="categoryId"
            render={({ field }) => (
              <FormItem>
                <FormLabel>Category (Optional)</FormLabel>
                <FormControl>
                  <AsyncSelect<ProductCategoryDto>
                    label="Category"
                    placeholder="Select category..."
                    fetcher={fetchProductCategoriesForSelect}
                    value={field.value ? String(field.value) : ''}
                    onChange={(v) => field.onChange(v ? Number(v) : undefined)}
                    getOptionValue={(c) => String(c.id)}
                    getDisplayValue={(c) => c.name}
                    renderOption={(c) => (
                      <span className="font-medium">{c.name}</span>
                    )}
                    clearable
                    width="100%"
                  />
                </FormControl>
                <FormMessage />
              </FormItem>
            )}
          />
        </div>

        {/* Hidden concurrency token — edit mode only */}
        {mode === 'edit' && (
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
          ) : mode === 'create' ? (
            'Create Product'
          ) : (
            'Update Product'
          )}
        </Button>
      </form>
    </Form>
  )
}
