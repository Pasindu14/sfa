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
}
