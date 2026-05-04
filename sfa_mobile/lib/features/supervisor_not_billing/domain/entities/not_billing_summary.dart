import 'package:equatable/equatable.dart';
import 'package:uswatte/features/supervisor_not_billing/domain/entities/not_billing_reason.dart';

class NotBillingSummary extends Equatable {
  final int id;
  final int outletId;
  final int salesRepId;
  final String notBillingNumber;
  final String notBillingDate;
  final String outletName;
  final String salesRepName;
  final NotBillingReason reason;
  final DateTime createdAt;

  const NotBillingSummary({
    required this.id,
    required this.outletId,
    required this.salesRepId,
    required this.notBillingNumber,
    required this.notBillingDate,
    required this.outletName,
    required this.salesRepName,
    required this.reason,
    required this.createdAt,
  });

  @override
  List<Object?> get props => [
        id,
        outletId,
        salesRepId,
        notBillingNumber,
        notBillingDate,
        outletName,
        salesRepName,
        reason,
        createdAt,
      ];
}
