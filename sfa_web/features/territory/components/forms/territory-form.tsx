'use client'

import { useEffect } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import {
  createTerritorySchema,
  updateTerritorySchema,
  type CreateTerritoryInput,
} from '../../schema/territory.schema'
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
import { AreaSelect } from '@/features/area/components/selects/area-select'

interface TerritoryFormProps {
  mode: 'create' | 'edit'
  defaultValues?: Partial<CreateTerritoryInput>
  onSubmit: (data: CreateTerritoryInput) => void
  isLoading: boolean
  fieldErrors?: Record<string, string> | null
}

export function TerritoryForm({
  mode,
  defaultValues,
  onSubmit,
  isLoading,
  fieldErrors,
}: TerritoryFormProps) {
  const schema = mode === 'create' ? createTerritorySchema : updateTerritorySchema

  const form = useForm<CreateTerritoryInput>({
    resolver: zodResolver(schema as typeof createTerritorySchema),
    defaultValues: {
      name: '',
      areaId: 0,
      ...defaultValues,
    },
  })

  const { setError } = form

  useEffect(() => {
    if (fieldErrors) {
      Object.entries(fieldErrors).forEach(([field, message]) => {
        setError(field as keyof CreateTerritoryInput, { message })
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
                <Input placeholder="Territory name" {...field} />
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />

        <FormField
          control={form.control}
          name="areaId"
          render={({ field }) => (
            <FormItem>
              <FormLabel>Area</FormLabel>
              <FormControl>
                <AreaSelect
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
            'Create Territory'
          ) : (
            'Update Territory'
          )}
        </Button>
      </form>
    </Form>
  )
}
