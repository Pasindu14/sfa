import { z } from 'zod'

export const roleEnum = z.enum(['Admin', 'SalesRep', 'Manager'])

const passwordRules = z
  .string()
  .min(8, 'Password must be at least 8 characters')
  .regex(/[A-Z]/, 'Password must contain an uppercase letter')
  .regex(/[a-z]/, 'Password must contain a lowercase letter')
  .regex(/[0-9]/, 'Password must contain a number')
  .regex(/[^A-Za-z0-9]/, 'Password must contain a special character')

export const createUserSchema = z.object({
  name: z.string().min(1, 'Name is required'),
  username: z.string().min(1, 'Username is required'),
  email: z.string().email('Invalid email address'),
  phone: z.string().min(1, 'Phone is required'),
  password: passwordRules,
  role: roleEnum,
  deviceId: z.string().optional(),
})

export const updateUserSchema = z.object({
  name: z.string().min(1, 'Name is required'),
  username: z.string().min(1, 'Username is required'),
  email: z.string().email('Invalid email address'),
  phone: z.string().min(1, 'Phone is required'),
  role: roleEnum,
  deviceId: z.string().optional(),
})

export const changePasswordSchema = z
  .object({
    newPassword: passwordRules,
    confirmPassword: z.string().min(1, 'Please confirm your password'),
  })
  .superRefine((data, ctx) => {
    if (data.newPassword !== data.confirmPassword) {
      ctx.addIssue({
        code: z.ZodIssueCode.custom,
        message: 'Passwords do not match',
        path: ['confirmPassword'],
      })
    }
  })

export const filterSchema = z.object({
  search: z.string().optional(),
  role: z.string().optional(),
  isActive: z.string().optional(),
  page: z.number().default(1),
  pageSize: z.number().default(10),
})

export type CreateUserInput = z.infer<typeof createUserSchema>
export type UpdateUserInput = z.infer<typeof updateUserSchema>
export type ChangePasswordInput = z.infer<typeof changePasswordSchema>
export type UserFilterInput = z.infer<typeof filterSchema>

export type UserDto = {
  id: number
  name: string
  username: string
  email: string
  phone: string
  role: 'Admin' | 'SalesRep' | 'Manager'
  deviceId?: string
  isActive: boolean
  createdAt: string
  updatedAt: string
}
