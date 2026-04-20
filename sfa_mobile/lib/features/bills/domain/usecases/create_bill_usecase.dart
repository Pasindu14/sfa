import 'package:uswatte/features/bills/domain/entities/bill.dart';
import 'package:uswatte/features/bills/domain/repositories/bills_repository.dart';

class CreateBillUseCase {
  final BillsRepository _repo;
  const CreateBillUseCase(this._repo);

  Future<Bill> call(Bill bill) => _repo.createBill(bill);
}
