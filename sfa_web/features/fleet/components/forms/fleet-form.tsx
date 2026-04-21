'use client'

import { useEffect } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import {
  createFleetSchema,
  updateFleetSchema,
  type CreateFleetInput,
} from '../../schema/fleet.schema'
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

interface FleetFormProps {
  mode: 'create' | 'edit'
  defaultValues?: Partial<CreateFleetInput>
  onSubmit: (data: CreateFleetInput) => void
  isLoading: boolean
  fieldErrors?: Record<string, string> | null
}

export function FleetForm({
  mode,
  defaultValues,
  onSubmit,
  isLoading,
  fieldErrors,
}: FleetFormProps) {
  const schema = mode === 'create' ? createFleetSchema : updateFleetSchema

  const form = useForm<CreateFleetInput>({
    resolver: zodResolver(schema),
    defaultValues: {
      name: '',
      ...defaultValues,
    },
  })

  const { setError } = form

  useEffect(() => {
    if (fieldErrors) {
      Object.entries(fieldErrors).forEach(([field, message]) => {
        setError(field as keyof CreateFleetInput, { message })
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
                <Input placeholder="Enter fleet name" {...field} />
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />

        <Button type="submit" className="w-full" disabled={isLoading}>
          {isLoading ? (
            <Spinner className="mr-2" />
          ) : mode === 'create' ? (
            'Create Fleet'
          ) : (
            'Update Fleet'
          )}
        </Button>
      </form>
    </Form>
  )
}
