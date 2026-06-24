'use client'

import { useEffect } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import {
  createUserSchema,
  updateUserSchema,
  type CreateUserInput,
  type UpdateUserInput,
} from '../../schema/user.schema'
import { useDistributorsForSelect } from '@/features/distributor/hooks/distributor.hooks'
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
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { Spinner } from '@/components/ui/spinner'

// Form value type covers both modes: password is create-only, rowVersion is edit-only.
type UserFormValues = UpdateUserInput & { password?: string }

interface UserFormProps {
  mode: 'create' | 'edit'
  defaultValues?: Partial<UserFormValues>
  onSubmit: (data: CreateUserInput | UpdateUserInput) => void
  isLoading: boolean
  fieldErrors?: Record<string, string> | null
}

export function UserForm({
  mode,
  defaultValues,
  onSubmit,
  isLoading,
  fieldErrors,
}: UserFormProps) {
  const schema = mode === 'create' ? createUserSchema : updateUserSchema

  const form = useForm<UserFormValues>({
    resolver: zodResolver(schema as typeof updateUserSchema),
    defaultValues: {
      name: '',
      username: '',
      email: '',
      phone: '',
      password: '',
      role: 'SalesRep',
      deviceId: '',
      rowVersion: 0,
      ...defaultValues,
    },
  })

  const { setError, setValue, watch } = form
  const watchedRole = watch('role')
  const { distributors, isLoading: isLoadingDistributors } = useDistributorsForSelect()

  useEffect(() => {
    if (watchedRole !== 'Distributor') {
      setValue('distributorId', undefined)
    }
  }, [watchedRole, setValue])

  useEffect(() => {
    if (fieldErrors) {
      Object.entries(fieldErrors).forEach(([field, message]) => {
        setError(field as keyof UserFormValues, { message })
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
                <Input placeholder="Full name" {...field} />
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />

        <FormField
          control={form.control}
          name="username"
          render={({ field }) => (
            <FormItem>
              <FormLabel>Username</FormLabel>
              <FormControl>
                <Input placeholder="Username" {...field} />
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

        <FormField
          control={form.control}
          name="phone"
          render={({ field }) => (
            <FormItem>
              <FormLabel>Phone</FormLabel>
              <FormControl>
                <Input placeholder="+1234567890" {...field} />
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />

        <FormField
          control={form.control}
          name="role"
          render={({ field }) => (
            <FormItem>
              <FormLabel>Role</FormLabel>
              <Select onValueChange={field.onChange} value={field.value}>
                <FormControl>
                  <SelectTrigger>
                    <SelectValue placeholder="Select role" />
                  </SelectTrigger>
                </FormControl>
                <SelectContent>
                  <SelectItem value="Admin">Admin</SelectItem>
                  <SelectItem value="NSM">NSM (Head of Sales)</SelectItem>
                  <SelectItem value="RSM">RSM (Regional Sales Manager)</SelectItem>
                  <SelectItem value="ASM">ASM (Area Sales Manager)</SelectItem>
                  <SelectItem value="Supervisor">Supervisor (Manager)</SelectItem>
                  <SelectItem value="SalesRep">Sales Rep (Rep)</SelectItem>
                  <SelectItem value="Distributor">Distributor</SelectItem>
                </SelectContent>
              </Select>
              <FormMessage />
            </FormItem>
          )}
        />

        {watchedRole === 'Distributor' && (
          <FormField
            control={form.control}
            name="distributorId"
            render={({ field }) => (
              <FormItem>
                <FormLabel>Distributor</FormLabel>
                <Select
                  onValueChange={(val) => field.onChange(Number(val))}
                  value={field.value?.toString() ?? ''}
                  disabled={isLoadingDistributors}
                >
                  <FormControl>
                    <SelectTrigger>
                      <SelectValue placeholder={isLoadingDistributors ? 'Loading...' : 'Select distributor'} />
                    </SelectTrigger>
                  </FormControl>
                  <SelectContent>
                    {distributors.map((d) => (
                      <SelectItem key={d.id} value={d.id.toString()}>
                        {d.name}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
                <FormMessage />
              </FormItem>
            )}
          />
        )}

        <FormField
          control={form.control}
          name="deviceId"
          render={({ field }) => (
            <FormItem>
              <FormLabel>Device ID (optional)</FormLabel>
              <FormControl>
                <Input placeholder="Device ID" {...field} />
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />

        {mode === 'create' && (
          <FormField
            control={form.control}
            name="password"
            render={({ field }) => (
              <FormItem>
                <FormLabel>Password</FormLabel>
                <FormControl>
                  <Input type="password" placeholder="Password" {...field} />
                </FormControl>
                <FormMessage />
              </FormItem>
            )}
          />
        )}

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
            'Create User'
          ) : (
            'Update User'
          )}
        </Button>
      </form>
    </Form>
  )
}
