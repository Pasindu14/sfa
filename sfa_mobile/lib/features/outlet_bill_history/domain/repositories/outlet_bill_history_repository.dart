import 'package:uswatte/features/outlet_bill_history/domain/entities/outlet_bill_detail.dart';
import 'package:uswatte/features/outlet_bill_history/domain/entities/outlet_bill_summary.dart';

abstract class OutletBillHistoryRepository {
  Future<({List<OutletBillSummary> bills, bool hasMore})> getBillsForOutlet({
    required int outletId,
    required String dateFrom,
    required String dateTo,
    required int page,
    int pageSize,
  });

  Future<OutletBillDetail> getBillDetail(int billingId);
}
