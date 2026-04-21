'use client'

import { useEffect } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import {
  createProductCategorySchema,
  updateProductCategorySchema,
  type CreateProductCategoryInput,
} from '../../schema/product-category.schema'
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

interface ProductCategoryFormProps {
  mode: 'create' | 'edit'
  defaultValues?: Partial<CreateProductCategoryInput>
  onSubmit: (data: CreateProductCategoryInput) => void
  isLoading: boolean
  fieldErrors?: Record<string, string> | null
}

export function ProductCategoryForm({
  mode,
  defaultValues,
  onSubmit,
  isLoading,
  fieldErrors,
}: ProductCategoryFormProps) {
  const schema = mode === 'create' ? createProductCategorySchema : updateProductCategorySchema

  const form = useForm<CreateProductCategoryInput>({
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
        setError(field as keyof CreateProductCategoryInput, { message })
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
                <Input placeholder="e.g. Beverages" {...field} />
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />

        <Button type="submit" className="w-full" disabled={isLoading}>
          {isLoading ? (
            <Spinner className="mr-2" />
          ) : mode === 'create' ? (
            'Create Category'
          ) : (
            'Update Category'
          )}
        </Button>
      </form>
    </Form>
  )
}
