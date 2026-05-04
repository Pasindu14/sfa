import 'package:uswatte/features/supervisor_billing/domain/entities/billing_detail.dart';
import 'package:uswatte/features/supervisor_billing/domain/entities/billing_summary.dart';

abstract class SupervisorBillingRepository {
  Future<List<BillingSummary>> getSupervisorBillings({
    required int salesRepId,
    required String date,
    int page = 1,
    int pageSize = 50,
  });

  Future<BillingDetail> getBillingDetail(int id);
}
