// Shared formatting utilities for the purchase-order feature.
// The Intl.NumberFormat constructor is expensive — instantiate once at module level.

const currencyFormatter = new Intl.NumberFormat('en-LK', {
  style: 'currency',
  currency: 'LKR',
  minimumFractionDigits: 2,
})

export function formatCurrency(amount: number): string {
  return currencyFormatter.format(amount)
}
