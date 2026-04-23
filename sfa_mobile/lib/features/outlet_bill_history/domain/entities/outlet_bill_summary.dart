class OutletBillSummary {
  final int id;
  final String billingNumber;
  final DateTime billingDate;
  final int outletId;
  final String outletName;
  final String salesRepName;
  final String distributorName;
  final double totalAmount;
  final String status;
  final DateTime createdAt;

  const OutletBillSummary({
    required this.id,
    required this.billingNumber,
    required this.billingDate,
    required this.outletId,
    required this.outletName,
    required this.salesRepName,
    required this.distributorName,
    required this.totalAmount,
    required this.status,
    required this.createdAt,
  });
}
