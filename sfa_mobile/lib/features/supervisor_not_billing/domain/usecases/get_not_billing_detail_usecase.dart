import 'package:uswatte/features/supervisor_not_billing/domain/entities/rep_not_billing_detail.dart';
import 'package:uswatte/features/supervisor_not_billing/domain/repositories/supervisor_not_billing_repository.dart';

class GetNotBillingDetailUseCase {
  final SupervisorNotBillingRepository _repo;
  GetNotBillingDetailUseCase(this._repo);

  Future<RepNotBillingDetail> call(int id) => _repo.getNotBillingDetail(id);
}
