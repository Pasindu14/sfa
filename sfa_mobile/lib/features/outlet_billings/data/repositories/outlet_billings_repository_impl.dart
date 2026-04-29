import 'package:uswatte/features/outlet_billings/data/datasources/outlet_billings_remote_datasource.dart';
import 'package:uswatte/features/outlet_billings/domain/entities/assigned_route.dart';
import 'package:uswatte/features/outlet_billings/domain/entities/outlet_billing_summary.dart';
import 'package:uswatte/features/outlet_billings/domain/repositories/outlet_billings_repository.dart';

class OutletBillingsRepositoryImpl implements OutletBillingsRepository {
  final OutletBillingsRemoteDatasource _remote;

  const OutletBillingsRepositoryImpl(this._remote);

  @override
  Future<List<AssignedRoute>> getAssignedRoutes() async {
    final models = await _remote.getAssignedRoutes();
    return models.map((m) => m.toEntity()).toList();
  }

  @override
  Future<List<OutletBillingSummary>> getOutletSummary({
    required int routeId,
    required String dateFrom,
    required String dateTo,
  }) async {
    final models = await _remote.getOutletSummary(
      routeId: routeId,
      dateFrom: dateFrom,
      dateTo: dateTo,
    );
    return models.map((m) => m.toEntity()).toList();
  }
}
