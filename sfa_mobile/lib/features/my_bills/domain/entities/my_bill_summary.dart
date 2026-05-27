class MyBillSummary {
  final int id;
  final String billingNumber;
  final DateTime billingDate;
  final int outletId;
  final String outletName;
  final String distributorName;
  final double totalAmount;
  final String repStatus;
  final String distributorStatus;
  final DateTime createdAt;

  const MyBillSummary({
    required this.id,
    required this.billingNumber,
    required this.billingDate,
    required this.outletId,
    required this.outletName,
    required this.distributorName,
    required this.totalAmount,
    required this.repStatus,
    required this.distributorStatus,
    required this.createdAt,
  });
}
