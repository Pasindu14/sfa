import 'package:equatable/equatable.dart';
import 'package:uswatte/features/supervisor_not_billing/domain/entities/not_billing_reason.dart';

class RepNotBillingDetail extends Equatable {
  final int id;
  final int outletId;
  final int salesRepId;
  final String notBillingNumber;
  final String notBillingDate;
  final String outletName;
  final String salesRepName;
  final String? supervisorName;
  final NotBillingReason reason;
  final String? notes;
  final DateTime createdAt;

  const RepNotBillingDetail({
    required this.id,
    required this.outletId,
    required this.salesRepId,
    required this.notBillingNumber,
    required this.notBillingDate,
    required this.outletName,
    required this.salesRepName,
    this.supervisorName,
    required this.reason,
    this.notes,
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
        supervisorName,
        reason,
        notes,
        createdAt,
      ];
}
