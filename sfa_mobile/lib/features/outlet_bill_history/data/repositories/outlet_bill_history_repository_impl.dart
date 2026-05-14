import 'package:uswatte/features/outlet_bill_history/data/datasources/outlet_bill_history_remote_datasource.dart';
import 'package:uswatte/features/outlet_bill_history/domain/entities/outlet_bill_detail.dart';
import 'package:uswatte/features/outlet_bill_history/domain/entities/outlet_bill_summary.dart';
import 'package:uswatte/features/outlet_bill_history/domain/repositories/outlet_bill_history_repository.dart';

class OutletBillHistoryRepositoryImpl implements OutletBillHistoryRepository {
  final OutletBillHistoryRemoteDatasource _remote;

  const OutletBillHistoryRepositoryImpl(this._remote);

  @override
  Future<({List<OutletBillSummary> bills, bool hasMore})> getBillsForOutlet({
    required int outletId,
    required String dateFrom,
    required String dateTo,
    required int page,
    int pageSize = 20,
  }) async {
    final result = await _remote.getBillsForOutlet(
      outletId: outletId,
      dateFrom: dateFrom,
      dateTo: dateTo,
      page: page,
      pageSize: pageSize,
    );
    return (
      bills: result.bills.map((m) => m.toEntity()).toList(),
      hasMore: result.hasMore,
    );
  }

  @override
  Future<OutletBillDetail> getBillDetail(int billingId) async {
    final model = await _remote.getBillDetail(billingId);
    return model.toEntity();
  }
}
