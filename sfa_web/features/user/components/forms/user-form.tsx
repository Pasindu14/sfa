'use client'

import { useEffect } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import {
  createUserSchema,
  updateUserSchema,
  type CreateUserInput,
} from '../../schema/user.schema'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { Spinner } from '@/components/ui/spinner'

interface UserFormProps {
  mode: 'create' | 'edit'
  defaultValues?: Partial<CreateUserInput>
  onSubmit: (data: CreateUserInput) => void
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

  const form = useForm<CreateUserInput>({
    resolver: zodResolver(schema as typeof createUserSchema),
    defaultValues: {
      name: '',
      username: '',
      email: '',
      phone: '',
      password: '',
      role: 'SalesRep',
      deviceId: '',
      ...defaultValues,
    },
  })

  const {
    register,
    handleSubmit,
    setValue,
    watch,
    setError,
    formState: { errors },
  } = form

  useEffect(() => {
    if (fieldErrors) {
      Object.entries(fieldErrors).forEach(([field, message]) => {
        setError(field as keyof CreateUserInput, { message })
      })
    }
  }, [fieldErrors, setError])

  return (
    <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
      <div className="space-y-1">
        <Label htmlFor="name">Name</Label>
        <Input id="name" placeholder="Full name" {...register('name')} />
        {errors.name && (
          <p className="text-sm text-destructive">{errors.name.message}</p>
        )}
      </div>

      <div className="space-y-1">
        <Label htmlFor="username">Username</Label>
        <Input id="username" placeholder="Username" {...register('username')} />
        {errors.username && (
          <p className="text-sm text-destructive">{errors.username.message}</p>
        )}
      </div>

      <div className="space-y-1">
        <Label htmlFor="email">Email</Label>
        <Input id="email" type="email" placeholder="email@example.com" {...register('email')} />
        {errors.email && (
          <p className="text-sm text-destructive">{errors.email.message}</p>
        )}
      </div>

      <div className="space-y-1">
        <Label htmlFor="phone">Phone</Label>
        <Input id="phone" placeholder="+1234567890" {...register('phone')} />
        {errors.phone && (
          <p className="text-sm text-destructive">{errors.phone.message}</p>
        )}
      </div>

      <div className="space-y-1">
        <Label htmlFor="role">Role</Label>
        <Select
          value={watch('role')}
          onValueChange={(val) =>
            setValue('role', val as CreateUserInput['role'], { shouldValidate: true })
          }
        >
          <SelectTrigger id="role">
            <SelectValue placeholder="Select role" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="Admin">Admin</SelectItem>
            <SelectItem value="SalesRep">Sales Rep</SelectItem>
            <SelectItem value="Manager">Manager</SelectItem>
          </SelectContent>
        </Select>
        {errors.role && (
          <p className="text-sm text-destructive">{errors.role.message}</p>
        )}
      </div>

      <div className="space-y-1">
        <Label htmlFor="deviceId">Device ID (optional)</Label>
        <Input id="deviceId" placeholder="Device ID" {...register('deviceId')} />
        {errors.deviceId && (
          <p className="text-sm text-destructive">{errors.deviceId.message}</p>
        )}
      </div>

      {mode === 'create' && (
        <div className="space-y-1">
          <Label htmlFor="password">Password</Label>
          <Input
            id="password"
            type="password"
            placeholder="Password"
            {...register('password')}
          />
          {errors.password && (
            <p className="text-sm text-destructive">{errors.password.message}</p>
          )}
        </div>
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
  )
}
