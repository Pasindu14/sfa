'use client'

import { useEffect } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import {
  createDivisionSchema,
  updateDivisionSchema,
  type UpdateDivisionInput,
} from '../../schema/division.schema'
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
import { TerritorySelect } from '@/features/territory/components/selects/territory-select'

// UpdateDivisionInput is a superset of CreateDivisionInput (adds rowVersion).
interface DivisionFormProps {
  mode: 'create' | 'edit'
  defaultValues?: Partial<UpdateDivisionInput>
  onSubmit: (data: UpdateDivisionInput) => void
  isLoading: boolean
  fieldErrors?: Record<string, string> | null
}

export function DivisionForm({
  mode,
  defaultValues,
  onSubmit,
  isLoading,
  fieldErrors,
}: DivisionFormProps) {
  const schema = mode === 'create' ? createDivisionSchema : updateDivisionSchema

  const form = useForm<UpdateDivisionInput>({
    resolver: zodResolver(schema as typeof updateDivisionSchema),
    defaultValues: {
      name: '',
      territoryId: 0,
      rowVersion: 0,
      ...defaultValues,
    },
  })

  const { setError } = form

  useEffect(() => {
    if (fieldErrors) {
      Object.entries(fieldErrors).forEach(([field, message]) => {
        setError(field as keyof UpdateDivisionInput, { message })
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
                <Input placeholder="Division name" {...field} />
              </FormControl>
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
                <TerritorySelect
                  value={field.value ? String(field.value) : ''}
                  onValueChange={(value) => field.onChange(Number(value))}
                  disabled={isLoading}
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
          {isLoading ? (
            <Spinner className="mr-2" />
          ) : mode === 'create' ? (
            'Create Division'
          ) : (
            'Update Division'
          )}
        </Button>
      </form>
    </Form>
  )
}
