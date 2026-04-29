import 'package:uswatte/features/outlet_billings/domain/entities/assigned_route.dart';
import 'package:uswatte/features/outlet_billings/domain/entities/outlet_billing_summary.dart';

abstract class OutletBillingsRepository {
  Future<List<AssignedRoute>> getAssignedRoutes();

  Future<List<OutletBillingSummary>> getOutletSummary({
    required int routeId,
    required String dateFrom,
    required String dateTo,
  });
}
