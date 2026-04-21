'use client'

import { useEffect } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import {
  createProductSchema,
  updateProductSchema,
  type CreateProductInput,
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
import type { FleetDto } from '@/features/fleet/schema/fleet.schema'

interface ProductFormProps {
  mode: 'create' | 'edit'
  defaultValues?: Partial<CreateProductInput>
  onSubmit: (data: CreateProductInput) => void
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

  const form = useForm<CreateProductInput>({
    resolver: zodResolver(schema),
    defaultValues: {
      code: '',
      itemDescription: '',
      printDescription: '',
      piecesPerPack: 0,
      imageUrl: '',
      remarks: '',
      fleetId: undefined,
      ...defaultValues,
    },
  })

  const { setError } = form

  useEffect(() => {
    if (fieldErrors) {
      Object.entries(fieldErrors).forEach(([field, message]) => {
        setError(field as keyof CreateProductInput, { message })
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
                    onChange={(e) =>
                      field.onChange(e.target.value !== '' ? parseInt(e.target.value, 10) : 0)
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
