import 'package:uswatte/features/bills/domain/entities/bill.dart';
import 'package:uswatte/features/bills/domain/repositories/bills_repository.dart';

class GetBillsUseCase {
  final BillsRepository _repo;
  const GetBillsUseCase(this._repo);

  Future<List<Bill>> call({int? limit}) => _repo.getBills(limit: limit);
}
