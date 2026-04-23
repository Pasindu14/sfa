import 'package:uswatte/features/not_billings/domain/repositories/not_billings_repository.dart';

class RetryNotBillingSyncUseCase {
  final NotBillingsRepository _repository;

  const RetryNotBillingSyncUseCase(this._repository);

  Future<void> call(String clientNotBillingId) =>
      _repository.retrySync(clientNotBillingId);
}
