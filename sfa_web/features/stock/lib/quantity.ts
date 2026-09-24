// Stock quantities are stored in pieces. piecesPerPack (0 = no pack size configured) splits a
// quantity into cases + leftover pieces, matching the "CS · PCS" convention on the stock page.

export function splitCasesPieces(qty: number, piecesPerPack: number): { cases: number; pieces: number } {
  if (piecesPerPack <= 0) return { cases: 0, pieces: qty }
  const cases = Math.floor(qty / piecesPerPack)
  return { cases, pieces: qty - cases * piecesPerPack }
}

export function formatCasesPieces(qty: number, piecesPerPack: number): string {
  if (qty < 0) return `-${formatCasesPieces(-qty, piecesPerPack)}`
  if (piecesPerPack <= 0) return `${qty} PCS`
  const { cases, pieces } = splitCasesPieces(qty, piecesPerPack)
  return `${cases} CS · ${pieces} PCS`
}

// Like formatCasesPieces but drops a zero part: "7 PCS", "2 CS", "2 CS · 7 PCS" — for tight layouts.
export function formatCasesPiecesCompact(qty: number, piecesPerPack: number): string {
  if (qty < 0) return `-${formatCasesPiecesCompact(-qty, piecesPerPack)}`
  if (piecesPerPack <= 0) return `${qty} PCS`
  const { cases, pieces } = splitCasesPieces(qty, piecesPerPack)
  if (cases === 0) return `${pieces} PCS`
  if (pieces === 0) return `${cases} CS`
  return `${cases} CS · ${pieces} PCS`
}

// Key identifying a stock row — a product can carry both a Normal and a FreeIssue balance.
export function stockLineKey(productId: number, stockType: string): string {
  return `${productId}:${stockType}`
}
