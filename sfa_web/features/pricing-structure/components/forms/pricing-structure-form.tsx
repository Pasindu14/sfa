'use client'

import { useEffect } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import {
  createPricingStructureSchema,
  updatePricingStructureSchema,
  type UpdatePricingStructureInput,
  type UpdatePricingStructureFormInput,
} from '../../schema/pricing-structure.schema'
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

// UpdatePricingStructureInput is a superset of the create/duplicate shape (adds rowVersion).
// Create and duplicate both submit { name, description }; edit also carries rowVersion.
interface PricingStructureFormProps {
  mode: 'create' | 'edit' | 'duplicate'
  defaultValues?: Partial<UpdatePricingStructureInput>
  onSubmit: (data: UpdatePricingStructureInput) => void
  isLoading: boolean
  fieldErrors?: Record<string, string> | null
}

const SUBMIT_LABEL = {
  create: 'Create Pricing Structure',
  edit: 'Update Pricing Structure',
  duplicate: 'Duplicate',
} as const

const EMPTY: UpdatePricingStructureFormInput = { name: '', description: '', rowVersion: 0 }

export function PricingStructureForm({
  mode,
  defaultValues,
  onSubmit,
  isLoading,
  fieldErrors,
}: PricingStructureFormProps) {
  const schema = mode === 'edit' ? updatePricingStructureSchema : createPricingStructureSchema

  // Three-generic useForm: fields work in the INPUT type, handleSubmit yields the parsed
  // OUTPUT type (name trimmed).
  const form = useForm<UpdatePricingStructureFormInput, unknown, UpdatePricingStructureInput>({
    resolver: zodResolver(schema as typeof updatePricingStructureSchema),
    defaultValues: { ...EMPTY, ...defaultValues },
  })

  const { setError, reset } = form

  useEffect(() => {
    if (defaultValues) reset({ ...EMPTY, ...defaultValues })
  }, [defaultValues, reset])

  useEffect(() => {
    if (fieldErrors) {
      Object.entries(fieldErrors).forEach(([field, message]) => {
        setError(field as keyof UpdatePricingStructureFormInput, { message })
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
              <FormLabel>Name *</FormLabel>
              <FormControl>
                <Input placeholder="e.g. Standard 2026" autoFocus {...field} />
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
              <FormLabel>Description (Optional)</FormLabel>
              <FormControl>
                <Textarea
                  placeholder="What this price list is for"
                  rows={3}
                  {...field}
                  value={field.value ?? ''}
                />
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />

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
          {isLoading ? <Spinner className="mr-2" /> : SUBMIT_LABEL[mode]}
        </Button>
      </form>
    </Form>
  )
}
