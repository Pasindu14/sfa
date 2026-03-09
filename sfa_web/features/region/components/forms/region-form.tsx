'use client'

import { useEffect } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import {
  createRegionSchema,
  updateRegionSchema,
  type CreateRegionInput,
} from '../../schema/region.schema'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import {
  Form,
  FormControl,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from '@/components/ui/form'
import { Spinner } from '@/components/ui/spinner'

interface RegionFormProps {
  mode: 'create' | 'edit'
  defaultValues?: Partial<CreateRegionInput>
  onSubmit: (data: CreateRegionInput) => void
  isLoading: boolean
  fieldErrors?: Record<string, string> | null
}

export function RegionForm({
  mode,
  defaultValues,
  onSubmit,
  isLoading,
  fieldErrors,
}: RegionFormProps) {
  const schema = mode === 'create' ? createRegionSchema : updateRegionSchema

  const form = useForm<CreateRegionInput>({
    resolver: zodResolver(schema as typeof createRegionSchema),
    defaultValues: {
      name: '',
      ...defaultValues,
    },
  })

  const { setError } = form

  useEffect(() => {
    if (fieldErrors) {
      Object.entries(fieldErrors).forEach(([field, message]) => {
        setError(field as keyof CreateRegionInput, { message })
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
                <Input placeholder="Region name" {...field} />
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />

        <Button type="submit" className="w-full" disabled={isLoading}>
          {isLoading ? (
            <Spinner className="mr-2" />
          ) : mode === 'create' ? (
            'Create Region'
          ) : (
            'Update Region'
          )}
        </Button>
      </form>
    </Form>
  )
}
