import 'package:uswatte/features/supervisor_not_billing/data/datasources/supervisor_not_billing_remote_datasource.dart';
import 'package:uswatte/features/supervisor_not_billing/domain/entities/not_billing_summary.dart';
import 'package:uswatte/features/supervisor_not_billing/domain/entities/rep_not_billing_detail.dart';
import 'package:uswatte/features/supervisor_not_billing/domain/repositories/supervisor_not_billing_repository.dart';

class SupervisorNotBillingRepositoryImpl
    implements SupervisorNotBillingRepository {
  final SupervisorNotBillingRemoteDatasource _datasource;
  SupervisorNotBillingRepositoryImpl(this._datasource);

  @override
  Future<List<NotBillingSummary>> getSupervisorNotBillings({
    required int salesRepId,
    required String date,
    int page = 1,
    int pageSize = 50,
  }) =>
      _datasource.getSupervisorNotBillings(
        salesRepId: salesRepId,
        date: date,
        page: page,
        pageSize: pageSize,
      );

  @override
  Future<RepNotBillingDetail> getNotBillingDetail(int id) =>
      _datasource.getNotBillingDetail(id);
}
