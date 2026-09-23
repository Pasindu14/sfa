// Stock quantities are stored in pieces. piecesPerPack (0 = no pack size configured) splits a
// quantity into cases + leftover pieces, matching the "CS · PCS" convention on the stock page.

export function splitCasesPieces(qty: number, piecesPerPack: number): { cases: number; pieces: number } {
  if (piecesPerPack <= 0) return { cases: 0, pieces: qty }
  const cases = Math.floor(qty / piecesPerPack)
  return { cases, pieces: qty - cases * piecesPerPack }
}

export function formatCasesPieces(qty: number, piecesPerPack: number): string {
  if (piecesPerPack <= 0) return `${qty} PCS`
  const { cases, pieces } = splitCasesPieces(qty, piecesPerPack)
  return `${cases} CS · ${pieces} PCS`
}

// Key identifying a stock row — a product can carry both a Normal and a FreeIssue balance.
export function stockLineKey(productId: number, stockType: string): string {
  return `${productId}:${stockType}`
}
