import 'package:equatable/equatable.dart';

enum BillingStatus { submitted, approved, cancelled }

class BillingSummary extends Equatable {
  final int id;
  final String billingNumber;
  final String billingDate;
  final int outletId;
  final String outletName;
  final int salesRepId;
  final String salesRepName;
  final int distributorId;
  final String distributorName;
  final double totalAmount;
  final BillingStatus status;
  final DateTime createdAt;

  const BillingSummary({
    required this.id,
    required this.billingNumber,
    required this.billingDate,
    required this.outletId,
    required this.outletName,
    required this.salesRepId,
    required this.salesRepName,
    required this.distributorId,
    required this.distributorName,
    required this.totalAmount,
    required this.status,
    required this.createdAt,
  });

  @override
  List<Object?> get props => [id];
}
