import 'package:uswatte/features/outlet_bill_history/domain/entities/outlet_bill_detail.dart';
import 'package:uswatte/features/outlet_bill_history/domain/entities/outlet_bill_summary.dart';

abstract class OutletBillHistoryRepository {
  Future<List<OutletBillSummary>> getBillsForOutlet({
    required int outletId,
    required String dateFrom,
    required String dateTo,
  });

  Future<OutletBillDetail> getBillDetail(int billingId);
}
