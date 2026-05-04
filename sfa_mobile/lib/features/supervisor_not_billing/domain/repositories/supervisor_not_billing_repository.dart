import 'package:uswatte/features/supervisor_not_billing/domain/entities/not_billing_summary.dart';
import 'package:uswatte/features/supervisor_not_billing/domain/entities/rep_not_billing_detail.dart';

abstract class SupervisorNotBillingRepository {
  Future<List<NotBillingSummary>> getSupervisorNotBillings({
    required int salesRepId,
    required String date,
    int page = 1,
    int pageSize = 50,
  });

  Future<RepNotBillingDetail> getNotBillingDetail(int id);
}
