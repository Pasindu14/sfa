import { z } from 'zod'

export const myGrnItemSchema = z.object({
  id: z.number(),
  productId: z.number(),
  productName: z.string(),
  productCode: z.string(),
  quantity: z.number(),
  unit: z.string(),
  notes: z.string().nullable(),
})

export const myGrnListItemSchema = z.object({
  id: z.number(),
  grnNumber: z.string(),
  salesInvoiceId: z.number(),
  salesInvoiceVchBillNo: z.string(),
  status: z.enum(['Pending', 'Confirmed', 'Disputed']),
  receivedAt: z.string().nullable(),
  confirmedBy: z.number().nullable(),
  confirmedByName: z.string().nullable(),
  confirmedAt: z.string().nullable(),
  notes: z.string().nullable(),
  createdAt: z.string(),
  items: z.array(myGrnItemSchema),
})

export const confirmMyGrnSchema = z.object({
  receivedAt: z.string().min(1, 'Received date is required'),
  notes: z.string().optional(),
})

export type MyGrnItem = z.infer<typeof myGrnItemSchema>
export type MyGrnListItem = z.infer<typeof myGrnListItemSchema>
export type ConfirmMyGrnInput = z.infer<typeof confirmMyGrnSchema>
export type MyGrnStatus = MyGrnListItem['status']
