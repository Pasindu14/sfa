'use client'

import { useEffect } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import {
  createRouteSchema,
  updateRouteSchema,
  type CreateRouteInput,
} from '../../schema/route.schema'
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
import { DivisionSelect } from '@/features/division/components/selects/division-select'

interface RouteFormProps {
  mode: 'create' | 'edit'
  defaultValues?: Partial<CreateRouteInput>
  onSubmit: (data: CreateRouteInput) => void
  isLoading: boolean
  fieldErrors?: Record<string, string> | null
}

export function RouteForm({
  mode,
  defaultValues,
  onSubmit,
  isLoading,
  fieldErrors,
}: RouteFormProps) {
  const schema = mode === 'create' ? createRouteSchema : updateRouteSchema

  const form = useForm<CreateRouteInput>({
    resolver: zodResolver(schema as typeof createRouteSchema),
    defaultValues: {
      name: '',
      pinColor: '#3b82f6',
      description: '',
      divisionId: 0,
      ...defaultValues,
    },
  })

  const { setError } = form

  useEffect(() => {
    if (fieldErrors) {
      Object.entries(fieldErrors).forEach(([field, message]) => {
        setError(field as keyof CreateRouteInput, { message })
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
                <Input placeholder="Route name" {...field} />
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />

        <FormField
          control={form.control}
          name="pinColor"
          render={({ field }) => (
            <FormItem>
              <FormLabel>Pin Color</FormLabel>
              <FormControl>
                <div className="flex items-center gap-3">
                  <input
                    type="color"
                    value={field.value ?? '#3b82f6'}
                    onChange={(e) => field.onChange(e.target.value)}
                    className="h-9 w-14 cursor-pointer rounded-md border border-input bg-background p-1"
                    aria-label="Pin color picker"
                  />
                  <Input
                    placeholder="#3b82f6"
                    value={field.value ?? ''}
                    onChange={(e) => field.onChange(e.target.value)}
                    className="flex-1"
                  />
                </div>
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
              <FormLabel>Description (optional)</FormLabel>
              <FormControl>
                <Textarea
                  placeholder="Route description"
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
          name="divisionId"
          render={({ field }) => (
            <FormItem>
              <FormLabel>Division</FormLabel>
              <FormControl>
                <DivisionSelect
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
            'Create Route'
          ) : (
            'Update Route'
          )}
        </Button>
      </form>
    </Form>
  )
}
