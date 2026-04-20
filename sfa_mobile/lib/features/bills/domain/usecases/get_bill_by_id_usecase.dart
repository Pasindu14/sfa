import 'package:uswatte/features/bills/domain/entities/bill.dart';
import 'package:uswatte/features/bills/domain/repositories/bills_repository.dart';

class GetBillByIdUseCase {
  final BillsRepository _repo;
  const GetBillByIdUseCase(this._repo);

  Future<Bill?> call(String clientBillId) => _repo.getBillById(clientBillId);
}
