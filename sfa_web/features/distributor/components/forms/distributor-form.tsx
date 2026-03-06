'use client'

import { useEffect } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import {
  createDistributorSchema,
  updateDistributorSchema,
  type CreateDistributorInput,
} from '../../schema/distributor.schema'
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

interface DistributorFormProps {
  mode: 'create' | 'edit'
  defaultValues?: Partial<CreateDistributorInput>
  onSubmit: (data: CreateDistributorInput) => void
  isLoading: boolean
  fieldErrors?: Record<string, string> | null
}

export function DistributorForm({
  mode,
  defaultValues,
  onSubmit,
  isLoading,
  fieldErrors,
}: DistributorFormProps) {
  const schema = mode === 'create' ? createDistributorSchema : updateDistributorSchema

  const form = useForm<CreateDistributorInput>({
    resolver: zodResolver(schema as typeof createDistributorSchema),
    defaultValues: {
      name: '',
      address: '',
      phone: '',
      email: '',
      alias: undefined,
      tradeDiscount: undefined,
      commission: undefined,
      remark: '',
      vatRegNo: '',
      latitude: undefined,
      longitude: undefined,
      ...defaultValues,
    },
  })

  const { setError } = form

  useEffect(() => {
    if (fieldErrors) {
      Object.entries(fieldErrors).forEach(([field, message]) => {
        setError(field as keyof CreateDistributorInput, { message })
      })
    }
  }, [fieldErrors, setError])

  return (
    <Form {...form}>
      <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
        <div className="grid grid-cols-2 gap-4">
          <FormField
            control={form.control}
            name="name"
            render={({ field }) => (
              <FormItem>
                <FormLabel>Name</FormLabel>
                <FormControl>
                  <Input placeholder="Enter distributor name" {...field} />
                </FormControl>
                <FormMessage />
              </FormItem>
            )}
          />

          <FormField
            control={form.control}
            name="alias"
            render={({ field }) => (
              <FormItem>
                <FormLabel>Alias (Numbers only)</FormLabel>
                <FormControl>
                  <Input type="number" placeholder="Enter alias (numbers only)" {...field} />
                </FormControl>
                <FormMessage />
              </FormItem>
            )}
          />
        </div>

        <FormField
          control={form.control}
          name="address"
          render={({ field }) => (
            <FormItem>
              <FormLabel>Address</FormLabel>
              <FormControl>
                <Textarea placeholder="Enter address" rows={2} {...field} />
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />

        <div className="grid grid-cols-2 gap-4">
          <FormField
            control={form.control}
            name="phone"
            render={({ field }) => (
              <FormItem>
                <FormLabel>Phone</FormLabel>
                <FormControl>
                  <Input placeholder="+1 234 567 8900" {...field} />
                </FormControl>
                <FormMessage />
              </FormItem>
            )}
          />

          <FormField
            control={form.control}
            name="email"
            render={({ field }) => (
              <FormItem>
                <FormLabel>Email</FormLabel>
                <FormControl>
                  <Input type="email" placeholder="email@example.com" {...field} />
                </FormControl>
                <FormMessage />
              </FormItem>
            )}
          />
        </div>

        <div className="grid grid-cols-2 gap-4">
          <FormField
            control={form.control}
            name="tradeDiscount"
            render={({ field }) => (
              <FormItem>
                <FormLabel>Trade Discount (%) *</FormLabel>
                <FormControl>
                  <Input type="number" step="0.01" min="0.01" placeholder="5.00" {...field} />
                </FormControl>
                <FormMessage />
              </FormItem>
            )}
          />

          <FormField
            control={form.control}
            name="commission"
            render={({ field }) => (
              <FormItem>
                <FormLabel>Commission (%) *</FormLabel>
                <FormControl>
                  <Input type="number" step="0.01" min="0.01" placeholder="2.50" {...field} />
                </FormControl>
                <FormMessage />
              </FormItem>
            )}
          />
        </div>

        <FormField
          control={form.control}
          name="vatRegNo"
          render={({ field }) => (
            <FormItem>
              <FormLabel>VAT Registration Number (Optional)</FormLabel>
              <FormControl>
                <Input placeholder="Enter VAT registration number" {...field} value={field.value || ''} />
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />

        <div className="grid grid-cols-2 gap-4">
          <FormField
            control={form.control}
            name="latitude"
            render={({ field }) => (
              <FormItem>
                <FormLabel>Latitude (Optional)</FormLabel>
                <FormControl>
                  <Input
                    type="number"
                    step="0.000001"
                    placeholder="37.7749"
                    {...field}
                    value={field.value ?? ''}
                    onChange={(e) => field.onChange(e.target.value ? parseFloat(e.target.value) : undefined)}
                  />
                </FormControl>
                <FormMessage />
              </FormItem>
            )}
          />

          <FormField
            control={form.control}
            name="longitude"
            render={({ field }) => (
              <FormItem>
                <FormLabel>Longitude (Optional)</FormLabel>
                <FormControl>
                  <Input
                    type="number"
                    step="0.000001"
                    placeholder="-122.4194"
                    {...field}
                    value={field.value ?? ''}
                    onChange={(e) => field.onChange(e.target.value ? parseFloat(e.target.value) : undefined)}
                  />
                </FormControl>
                <FormMessage />
              </FormItem>
            )}
          />
        </div>

        <FormField
          control={form.control}
          name="remark"
          render={({ field }) => (
            <FormItem>
              <FormLabel>Remark (Optional)</FormLabel>
              <FormControl>
                <Textarea placeholder="Enter any additional notes" rows={3} {...field} value={field.value || ''} />
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />

        <Button type="submit" className="w-full" disabled={isLoading}>
          {isLoading ? (
            <Spinner className="mr-2" />
          ) : mode === 'create' ? (
            'Create Distributor'
          ) : (
            'Update Distributor'
          )}
        </Button>
      </form>
    </Form>
  )
}
