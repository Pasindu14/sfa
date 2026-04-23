import 'package:uswatte/features/outlet_bill_history/domain/entities/outlet_bill_summary.dart';

class OutletBillSummaryModel {
  final int id;
  final String billingNumber;
  final String billingDate;
  final int outletId;
  final String outletName;
  final String salesRepName;
  final String distributorName;
  final double totalAmount;
  final String status;
  final String createdAt;

  const OutletBillSummaryModel({
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

  factory OutletBillSummaryModel.fromJson(Map<String, dynamic> json) =>
      OutletBillSummaryModel(
        id: json['id'] as int,
        billingNumber: json['billingNumber'] as String,
        billingDate: json['billingDate'] as String,
        outletId: json['outletId'] as int,
        outletName: json['outletName'] as String,
        salesRepName: json['salesRepName'] as String,
        distributorName: json['distributorName'] as String,
        totalAmount: (json['totalAmount'] as num).toDouble(),
        status: json['status'] as String,
        createdAt: json['createdAt'] as String,
      );

  OutletBillSummary toEntity() => OutletBillSummary(
        id: id,
        billingNumber: billingNumber,
        billingDate: DateTime.parse(billingDate),
        outletId: outletId,
        outletName: outletName,
        salesRepName: salesRepName,
        distributorName: distributorName,
        totalAmount: totalAmount,
        status: status,
        createdAt: DateTime.parse(createdAt),
      );
}
