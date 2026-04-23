import 'package:uswatte/features/outlet_bill_history/domain/entities/outlet_bill_item.dart';

class OutletBillDetail {
  final int id;
  final String billingNumber;
  final DateTime billingDate;
  final int outletId;
  final String outletName;
  final String salesRepName;
  final String distributorName;
  final double subTotalAmount;
  final double billDiscountRate;
  final double billDiscountAmount;
  final double totalAmount;
  final String status;
  final String? notes;
  final DateTime createdAt;
  final List<OutletBillItem> items;

  const OutletBillDetail({
    required this.id,
    required this.billingNumber,
    required this.billingDate,
    required this.outletId,
    required this.outletName,
    required this.salesRepName,
    required this.distributorName,
    required this.subTotalAmount,
    required this.billDiscountRate,
    required this.billDiscountAmount,
    required this.totalAmount,
    required this.status,
    this.notes,
    required this.createdAt,
    required this.items,
  });
}
