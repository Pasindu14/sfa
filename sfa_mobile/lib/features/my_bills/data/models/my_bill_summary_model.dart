import 'package:uswatte/features/my_bills/domain/entities/my_bill_summary.dart';

class MyBillSummaryModel {
  final int id;
  final String billingNumber;
  final String billingDate;
  final int outletId;
  final String outletName;
  final String distributorName;
  final double totalAmount;
  final String repStatus;
  final String distributorStatus;
  final String createdAt;

  const MyBillSummaryModel({
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

  factory MyBillSummaryModel.fromJson(Map<String, dynamic> json) =>
      MyBillSummaryModel(
        id: json['id'] as int,
        billingNumber: json['billingNumber'] as String,
        billingDate: json['billingDate'] as String,
        outletId: json['outletId'] as int,
        outletName: json['outletName'] as String,
        distributorName: json['distributorName'] as String,
        totalAmount: (json['totalAmount'] as num).toDouble(),
        repStatus: json['repStatus'] as String,
        distributorStatus: json['distributorStatus'] as String,
        createdAt: json['createdAt'] as String,
      );

  MyBillSummary toEntity() => MyBillSummary(
        id: id,
        billingNumber: billingNumber,
        billingDate: DateTime.parse(billingDate),
        outletId: outletId,
        outletName: outletName,
        distributorName: distributorName,
        totalAmount: totalAmount,
        repStatus: repStatus,
        distributorStatus: distributorStatus,
        createdAt: DateTime.parse(createdAt),
      );
}
