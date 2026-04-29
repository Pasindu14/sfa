import 'package:uswatte/features/outlet_billings/domain/entities/outlet_billing_summary.dart';
import 'package:uswatte/features/outlet_billings/domain/repositories/outlet_billings_repository.dart';

class GetOutletSummaryUseCase {
  final OutletBillingsRepository _repository;

  const GetOutletSummaryUseCase(this._repository);

  Future<List<OutletBillingSummary>> call({
    required int routeId,
    required String dateFrom,
    required String dateTo,
  }) =>
      _repository.getOutletSummary(
        routeId: routeId,
        dateFrom: dateFrom,
        dateTo: dateTo,
      );
}
