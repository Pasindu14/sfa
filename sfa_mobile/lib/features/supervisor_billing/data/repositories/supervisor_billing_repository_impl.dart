import 'package:uswatte/features/supervisor_billing/data/datasources/supervisor_billing_remote_datasource.dart';
import 'package:uswatte/features/supervisor_billing/domain/entities/billing_detail.dart';
import 'package:uswatte/features/supervisor_billing/domain/entities/billing_summary.dart';
import 'package:uswatte/features/supervisor_billing/domain/repositories/supervisor_billing_repository.dart';

class SupervisorBillingRepositoryImpl implements SupervisorBillingRepository {
  final SupervisorBillingRemoteDatasource _datasource;

  SupervisorBillingRepositoryImpl(this._datasource);

  @override
  Future<List<BillingSummary>> getSupervisorBillings({
    required int salesRepId,
    required String date,
    int page = 1,
    int pageSize = 50,
  }) =>
      _datasource.getSupervisorBillings(
        salesRepId: salesRepId,
        date: date,
        page: page,
        pageSize: pageSize,
      );

  @override
  Future<BillingDetail> getBillingDetail(int id) =>
      _datasource.getBillingDetail(id);
}
