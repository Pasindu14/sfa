import 'package:uswatte/features/bills/domain/repositories/bills_repository.dart';

class RetrySyncUseCase {
  final BillsRepository _repo;
  const RetrySyncUseCase(this._repo);

  Future<void> call(String clientBillId) => _repo.retrySync(clientBillId);
}
