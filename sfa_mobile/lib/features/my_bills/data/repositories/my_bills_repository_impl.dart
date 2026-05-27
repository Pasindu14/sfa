import 'package:uswatte/features/my_bills/data/datasources/my_bills_remote_datasource.dart';
import 'package:uswatte/features/my_bills/domain/entities/my_bill_summary.dart';
import 'package:uswatte/features/my_bills/domain/repositories/my_bills_repository.dart';

class MyBillsRepositoryImpl implements MyBillsRepository {
  final MyBillsRemoteDatasource _remote;

  const MyBillsRepositoryImpl(this._remote);

  @override
  Future<({List<MyBillSummary> bills, bool hasMore})> getMyBills({
    String? dateFrom,
    String? dateTo,
    String? billNo,
    required int page,
    int pageSize = 20,
  }) async {
    final result = await _remote.getMyBills(
      dateFrom: dateFrom,
      dateTo: dateTo,
      billNo: billNo,
      page: page,
      pageSize: pageSize,
    );
    return (
      bills: result.bills.map((m) => m.toEntity()).toList(),
      hasMore: result.hasMore,
    );
  }
}
