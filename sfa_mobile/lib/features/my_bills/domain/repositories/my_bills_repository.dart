import 'package:uswatte/features/my_bills/domain/entities/my_bill_summary.dart';

abstract class MyBillsRepository {
  Future<({List<MyBillSummary> bills, bool hasMore})> getMyBills({
    String? dateFrom,
    String? dateTo,
    String? billNo,
    required int page,
    int pageSize,
  });
}
