import 'package:uswatte/features/supervisor_not_billing/domain/entities/not_billing_summary.dart';
import 'package:uswatte/features/supervisor_not_billing/domain/repositories/supervisor_not_billing_repository.dart';

class GetSupervisorNotBillingsUseCase {
  final SupervisorNotBillingRepository _repo;
  GetSupervisorNotBillingsUseCase(this._repo);

  Future<List<NotBillingSummary>> call({
    required int salesRepId,
    required String date,
    int page = 1,
    int pageSize = 50,
  }) =>
      _repo.getSupervisorNotBillings(
        salesRepId: salesRepId,
        date: date,
        page: page,
        pageSize: pageSize,
      );
}
