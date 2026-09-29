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

/// Money + counts for a set of bills. A bill is a *sale* only once the
/// distributor has approved it (and the rep hasn't cancelled it).
class BillingTotals extends Equatable {
  final double salesAmount;
  final double pendingAmount;
  final int approvedCount;
  final int pendingCount;
  final int rejectedCount;
  final int cancelledCount;

  const BillingTotals({
    required this.salesAmount,
    required this.pendingAmount,
    required this.approvedCount,
    required this.pendingCount,
    required this.rejectedCount,
    required this.cancelledCount,
  });

  /// Everything still live — approved + awaiting approval. Rejected and
  /// cancelled bills are not business and are only reported as counts.
  double get totalBilled => salesAmount + pendingAmount;

  factory BillingTotals.from(Iterable<BillingSummary> bills) {
    var sales = 0.0, pending = 0.0;
    var approvedN = 0, pendingN = 0, rejectedN = 0, cancelledN = 0;
    for (final b in bills) {
      switch (b.status) {
        case BillingStatus.approved:
          sales += b.totalAmount;
          approvedN++;
        case BillingStatus.pending:
          pending += b.totalAmount;
          pendingN++;
        case BillingStatus.rejected:
          rejectedN++;
        case BillingStatus.cancelled:
          cancelledN++;
      }
    }
    return BillingTotals(
      salesAmount: sales,
      pendingAmount: pending,
      approvedCount: approvedN,
      pendingCount: pendingN,
      rejectedCount: rejectedN,
      cancelledCount: cancelledN,
    );
  }

  @override
  List<Object?> get props => [
        salesAmount,
        pendingAmount,
        approvedCount,
        pendingCount,
        rejectedCount,
        cancelledCount,
      ];
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
