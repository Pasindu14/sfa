import 'package:equatable/equatable.dart';

/// Effective status of a bill, collapsed from the API's two independent flags:
/// `repStatus` (Submitted/Cancelled) and `distributorStatus`
/// (Pending/Approved/Rejected).
enum BillingStatus {
  pending,
  approved,
  rejected,
  cancelled;

  /// Rep cancellation wins over everything — `CancelAsync` ignores the
  /// distributor status, so a bill can be Approved and then Cancelled, and it
  /// must not count as a sale.
  static BillingStatus fromApi({String? repStatus, String? distributorStatus}) {
    if (repStatus?.toLowerCase() == 'cancelled') return BillingStatus.cancelled;
    switch (distributorStatus?.toLowerCase()) {
      case 'approved':
        return BillingStatus.approved;
      case 'rejected':
        return BillingStatus.rejected;
      default:
        return BillingStatus.pending;
    }
  }

  static BillingStatus fromJson(Map<String, dynamic> json) => fromApi(
        repStatus: json['repStatus'] as String?,
        distributorStatus: json['distributorStatus'] as String?,
      );
}

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
