import 'package:uswatte/features/not_billings/domain/entities/not_billing.dart';
import 'package:uswatte/features/not_billings/domain/repositories/not_billings_repository.dart';

class GetNotBillingByIdUseCase {
  final NotBillingsRepository _repository;

  const GetNotBillingByIdUseCase(this._repository);

  Future<NotBilling?> call(String clientNotBillingId) =>
      _repository.getNotBillingById(clientNotBillingId);
}
