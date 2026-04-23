import 'package:uswatte/features/not_billings/domain/entities/not_billing.dart';
import 'package:uswatte/features/not_billings/domain/repositories/not_billings_repository.dart';

class GetNotBillingsUseCase {
  final NotBillingsRepository _repository;

  const GetNotBillingsUseCase(this._repository);

  Future<List<NotBilling>> call({int? limit}) => _repository.getNotBillings(limit: limit);
}
