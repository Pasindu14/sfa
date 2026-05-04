import 'package:uswatte/features/supervisor_billing/domain/entities/billing_summary.dart';
import 'package:uswatte/features/supervisor_billing/domain/repositories/supervisor_billing_repository.dart';

class GetSupervisorBillingsUseCase {
  final SupervisorBillingRepository _repository;

  GetSupervisorBillingsUseCase(this._repository);

  Future<List<BillingSummary>> call({
    required int salesRepId,
    required String date,
    int page = 1,
    int pageSize = 50,
  }) =>
      _repository.getSupervisorBillings(
        salesRepId: salesRepId,
        date: date,
        page: page,
        pageSize: pageSize,
      );
}
