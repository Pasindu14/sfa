import 'package:uswatte/features/not_billings/domain/repositories/not_billings_repository.dart';

class DeleteNotBillingUseCase {
  final NotBillingsRepository _repository;

  const DeleteNotBillingUseCase(this._repository);

  Future<void> call(String clientNotBillingId) =>
      _repository.deleteLocalNotBilling(clientNotBillingId);
}
