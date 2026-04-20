import 'package:uswatte/features/bills/domain/repositories/bills_repository.dart';

class DeleteBillUseCase {
  final BillsRepository _repo;
  const DeleteBillUseCase(this._repo);

  Future<void> call(String clientBillId) => _repo.deleteLocalBill(clientBillId);
}
