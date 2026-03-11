import { z } from 'zod'

export const roleEnum = z.enum(['Admin', 'SalesRep', 'Manager'])

const passwordRules = z
  .string()
  .min(8, 'Password must be at least 8 characters')
  .regex(/[A-Z]/, 'Password must contain at least one uppercase letter')
  .regex(/[a-z]/, 'Password must contain at least one lowercase letter')
  .regex(/[0-9]/, 'Password must contain at least one digit')
  .regex(/[^a-zA-Z0-9]/, 'Password must contain at least one special character')

const usernameRules = z
  .string()
  .min(3, 'Username must be at least 3 characters')
  .max(50, 'Username must not exceed 50 characters')
  .regex(/^[a-zA-Z0-9_]+$/, 'Username can only contain letters, numbers, and underscores')

const phoneRules = z
  .string()
  .min(10, 'Phone number must be at least 10 characters')
  .max(20, 'Phone number must not exceed 20 characters')
  .regex(/^[0-9+\-\s()]+$/, 'Phone number can only contain digits, +, -, spaces, and parentheses')

const baseUserSchema = z.object({
  name: z.string().min(1, 'Name is required').max(100, 'Name must not exceed 100 characters'),
  username: usernameRules,
  email: z.string().email('Invalid email format').max(255, 'Email must not exceed 255 characters'),
  phone: phoneRules,
  role: roleEnum,
  deviceId: z.string().optional(),
})

export const createUserSchema = baseUserSchema.extend({
  password: passwordRules,
})

export const updateUserSchema = baseUserSchema

export const changePasswordSchema = z
  .object({
    currentPassword: z.string().min(1, 'Current password is required'),
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
