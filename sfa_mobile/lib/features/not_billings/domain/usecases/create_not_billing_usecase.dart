import 'package:uswatte/features/not_billings/domain/entities/not_billing.dart';
import 'package:uswatte/features/not_billings/domain/repositories/not_billings_repository.dart';

class CreateNotBillingUseCase {
  final NotBillingsRepository _repository;

  const CreateNotBillingUseCase(this._repository);

  Future<NotBilling> call(NotBilling record) => _repository.createNotBilling(record);
}
