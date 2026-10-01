/// The money breakdown of one bill, in the order the rep reads it:
///
///   Sales (gross) − Discount − Returns = Total
///
/// [freeIssue] is informational only — it is never part of the arithmetic.
/// [discount] is every discount (line discounts plus the bill-level discount).
class BillBreakdown {
  final double gross;
  final double discount;
  final double returns;
  final double total;
  final double freeIssue;
  final double freeIssueCompany;
  final double freeIssueDistributor;

  const BillBreakdown({
    required this.gross,
    this.discount = 0,
    this.returns = 0,
    required this.total,
    this.freeIssue = 0,
    this.freeIssueCompany = 0,
    this.freeIssueDistributor = 0,
  });

  /// Breakdown for a list row. Every part is nullable so an older server that
  /// does not ship them yet still renders: gross falls back to [total] and the
  /// rest to zero, which hides the subline rather than showing wrong figures.
  factory BillBreakdown.forList({
    required double total,
    double? gross,
    double? discount,
    double? returns,
    double? freeIssue,
  }) => BillBreakdown(
    gross: gross ?? total,
    discount: discount ?? 0,
    returns: returns ?? 0,
    total: total,
    freeIssue: freeIssue ?? 0,
  );

  static const _epsilon = 0.004;

  bool get hasDiscount => discount > _epsilon;
  bool get hasReturns => returns > _epsilon;
  bool get hasFreeIssue => freeIssue > _epsilon;
  bool get hasFreeIssueSplit =>
      freeIssueCompany > _epsilon && freeIssueDistributor > _epsilon;

  /// Total discount as a share of gross sales.
  double get discountPercent => gross > 0 ? discount / gross * 100.0 : 0;

  /// Whether a list row has anything to say beyond the total.
  bool get hasListSubline => hasDiscount || hasReturns;
}
