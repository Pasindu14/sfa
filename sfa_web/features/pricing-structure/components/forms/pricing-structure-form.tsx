'use client'

import { useEffect } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import {
  createPricingStructureSchema,
  updatePricingStructureSchema,
  type CreatePricingStructureInput,
} from '../../schema/pricing-structure.schema'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Textarea } from '@/components/ui/textarea'
import { Checkbox } from '@/components/ui/checkbox'
import {
  Form,
  FormControl,
  FormDescription,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from '@/components/ui/form'
import { Spinner } from '@/components/ui/spinner'

interface PricingStructureFormProps {
  mode: 'create' | 'edit'
  defaultValues?: Partial<CreatePricingStructureInput>
  onSubmit: (data: CreatePricingStructureInput) => void
  isLoading: boolean
  fieldErrors?: Record<string, string> | null
}

export function PricingStructureForm({
  mode,
  defaultValues,
  onSubmit,
  isLoading,
  fieldErrors,
}: PricingStructureFormProps) {
  const schema = mode === 'create' ? createPricingStructureSchema : updatePricingStructureSchema

  const form = useForm<CreatePricingStructureInput>({
    resolver: zodResolver(schema as typeof createPricingStructureSchema),
    defaultValues: {
      name: '',
      description: '',
      isDefault: false,
      ...defaultValues,
    },
  })

  const { setError } = form

  useEffect(() => {
    if (fieldErrors) {
      Object.entries(fieldErrors).forEach(([field, message]) => {
        setError(field as keyof CreatePricingStructureInput, { message })
      })
    }
  }, [fieldErrors, setError])

  return (
    <Form {...form}>
      <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
        <FormField
          control={form.control}
          name="name"
          render={({ field }) => (
            <FormItem>
              <FormLabel>Name</FormLabel>
              <FormControl>
                <Input placeholder="e.g. Retail 2026" {...field} />
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />

        <FormField
          control={form.control}
          name="description"
          render={({ field }) => (
            <FormItem>
              <FormLabel>Description</FormLabel>
              <FormControl>
                <Textarea
                  placeholder="Optional description for this pricing structure"
                  className="resize-none"
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
          name="isDefault"
          render={({ field }) => (
            <FormItem className="flex flex-row items-start space-x-3 space-y-0 rounded-md border p-4">
              <FormControl>
                <Checkbox
                  checked={field.value}
                  onCheckedChange={field.onChange}
                />
              </FormControl>
              <div className="space-y-1 leading-none">
                <FormLabel>Set as default</FormLabel>
                <FormDescription>
                  This pricing structure will be pre-selected when creating invoices.
                  Only one structure can be the default at a time.
                </FormDescription>
              </div>
            </FormItem>
          )}
        />

        <Button type="submit" className="w-full" disabled={isLoading}>
          {isLoading ? (
            <Spinner className="mr-2" />
          ) : mode === 'create' ? (
            'Create Pricing Structure'
          ) : (
            'Update Pricing Structure'
          )}
        </Button>
      </form>
    </Form>
  )
}
