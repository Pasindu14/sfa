'use client'

import { useEffect } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import {
  createAreaSchema,
  updateAreaSchema,
  type CreateAreaInput,
} from '../../schema/area.schema'
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
import { RegionSelect } from '@/features/region/components/selects/region-select'

interface AreaFormProps {
  mode: 'create' | 'edit'
  defaultValues?: Partial<CreateAreaInput>
  onSubmit: (data: CreateAreaInput) => void
  isLoading: boolean
  fieldErrors?: Record<string, string> | null
}

export function AreaForm({
  mode,
  defaultValues,
  onSubmit,
  isLoading,
  fieldErrors,
}: AreaFormProps) {
  const schema = mode === 'create' ? createAreaSchema : updateAreaSchema

  const form = useForm<CreateAreaInput>({
    resolver: zodResolver(schema as typeof createAreaSchema),
    defaultValues: {
      name: '',
      regionId: 0,
      ...defaultValues,
    },
  })

  const { setError } = form

  useEffect(() => {
    if (fieldErrors) {
      Object.entries(fieldErrors).forEach(([field, message]) => {
        setError(field as keyof CreateAreaInput, { message })
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
                <Input placeholder="Area name" {...field} />
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />

        <FormField
          control={form.control}
          name="regionId"
          render={({ field }) => (
            <FormItem>
              <FormLabel>Region</FormLabel>
              <FormControl>
                <RegionSelect
                  value={field.value ? String(field.value) : ''}
                  onValueChange={(value) => field.onChange(Number(value))}
                  disabled={isLoading}
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
            'Create Area'
          ) : (
            'Update Area'
          )}
        </Button>
      </form>
    </Form>
  )
}
