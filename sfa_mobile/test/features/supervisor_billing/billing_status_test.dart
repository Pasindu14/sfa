import 'package:flutter_test/flutter_test.dart';
import 'package:uswatte/features/supervisor_billing/data/models/billing_summary_model.dart';
import 'package:uswatte/features/supervisor_billing/domain/entities/billing_summary.dart';

Map<String, dynamic> _json({
  int id = 1,
  double total = 100,
  String repStatus = 'Submitted',
  String distributorStatus = 'Pending',
}) =>
    {
      'id': id,
      'billingNumber': 'B-$id',
      'billingDate': '2026-09-29',
      'outletId': 1,
      'outletName': 'Outlet',
      'salesRepId': 7,
      'salesRepName': 'Rep',
      'supervisorName': null,
      'distributorId': 3,
      'distributorName': 'Dist',
      'totalAmount': total,
      'repStatus': repStatus,
      'distributorStatus': distributorStatus,
      'paymentType': 'Cash',
      'isCashCollected': false,
      'createdAt': '2026-09-29T04:00:00Z',
      'isAdjusted': false,
    };

void main() {
  group('BillingStatus.fromApi', () {
    test('maps distributor status when not cancelled', () {
      expect(BillingStatus.fromApi(repStatus: 'Submitted', distributorStatus: 'Pending'),
          BillingStatus.pending);
      expect(BillingStatus.fromApi(repStatus: 'Submitted', distributorStatus: 'Approved'),
          BillingStatus.approved);
      expect(BillingStatus.fromApi(repStatus: 'Submitted', distributorStatus: 'Rejected'),
          BillingStatus.rejected);
    });

    test('rep cancellation wins over distributor approval', () {
      expect(BillingStatus.fromApi(repStatus: 'Cancelled', distributorStatus: 'Approved'),
          BillingStatus.cancelled);
    });

    test('missing fields fall back to pending instead of throwing', () {
      expect(BillingStatus.fromApi(), BillingStatus.pending);
    });
  });

  test('BillingSummaryModel parses the real list DTO (no `status` field)', () {
    final model = BillingSummaryModel.fromJson(
        _json(repStatus: 'Submitted', distributorStatus: 'Approved'));
    expect(model.status, BillingStatus.approved);
  });

  test('BillingTotals counts only approved, non-cancelled bills as sales', () {
    final bills = [
      _json(id: 1, total: 1000, distributorStatus: 'Approved'),
      _json(id: 2, total: 250.5, distributorStatus: 'Pending'),
      _json(id: 3, total: 400, distributorStatus: 'Rejected'),
      _json(id: 4, total: 900, repStatus: 'Cancelled', distributorStatus: 'Approved'),
      _json(id: 5, total: 50, repStatus: 'Cancelled'),
    ].map(BillingSummaryModel.fromJson);

    final t = BillingTotals.from(bills);

    expect(t.salesAmount, 1000);
    expect(t.pendingAmount, 250.5);
    expect(t.totalBilled, 1250.5);
    expect(t.approvedCount, 1);
    expect(t.pendingCount, 1);
    expect(t.rejectedCount, 1);
    expect(t.cancelledCount, 2);
  });
}
