import 'package:uswatte/features/outlet_billings/domain/entities/bill_line.dart';

class BillLineModel {
  final int id;
  final String billingNumber;
  final String billingDate;
  final double totalAmount;
  final String status;

  const BillLineModel({
    required this.id,
    required this.billingNumber,
    required this.billingDate,
    required this.totalAmount,
    required this.status,
  });

  factory BillLineModel.fromJson(Map<String, dynamic> json) => BillLineModel(
        id: json['id'] as int,
        billingNumber: json['billingNumber'] as String,
        billingDate: json['billingDate'] as String,
        totalAmount: (json['totalAmount'] as num).toDouble(),
        status: json['status'] as String,
      );

  BillLine toEntity() => BillLine(
        id: id,
        billingNumber: billingNumber,
        billingDate: billingDate,
        totalAmount: totalAmount,
        status: status,
      );
}
