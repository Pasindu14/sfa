import 'package:uswatte/features/supervisor_billing/domain/entities/billing_detail.dart';
import 'package:uswatte/features/supervisor_billing/domain/repositories/supervisor_billing_repository.dart';

class GetBillingDetailUseCase {
  final SupervisorBillingRepository _repository;

  GetBillingDetailUseCase(this._repository);

  Future<BillingDetail> call(int id) => _repository.getBillingDetail(id);
}
