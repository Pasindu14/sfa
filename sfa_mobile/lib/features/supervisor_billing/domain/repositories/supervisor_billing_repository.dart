import 'package:uswatte/features/supervisor_billing/domain/entities/billing_detail.dart';
import 'package:uswatte/features/supervisor_billing/domain/entities/billing_summary.dart';

abstract class SupervisorBillingRepository {
  /// Every bill for the rep on [date] (all pages).
  Future<List<BillingSummary>> getSupervisorBillings({
    required int salesRepId,
    required String date,
  });

  Future<BillingDetail> getBillingDetail(int id);
}
