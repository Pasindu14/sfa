import 'package:uswatte/features/outlet_billings/domain/entities/assigned_route.dart';
import 'package:uswatte/features/outlet_billings/domain/entities/outlet_billing_summary.dart';

sealed class OutletBillingsState {
  const OutletBillingsState();
}

class OutletBillingsInitial extends OutletBillingsState {
  const OutletBillingsInitial();
}

class OutletBillingsRoutesLoading extends OutletBillingsState {
  const OutletBillingsRoutesLoading();
}

class OutletBillingsLoaded extends OutletBillingsState {
  final List<AssignedRoute> availableRoutes;
  final AssignedRoute? selectedRoute;
  final int monthOffset;
  final List<OutletBillingSummary> outletSummaries;
  final double grandTotal;
  final int totalBillingCount;
  final bool loadingOutlets;

  const OutletBillingsLoaded({
    required this.availableRoutes,
    required this.monthOffset,
    this.selectedRoute,
    this.outletSummaries = const [],
    this.grandTotal = 0,
    this.totalBillingCount = 0,
    this.loadingOutlets = false,
  });

  OutletBillingsLoaded copyWith({
    List<AssignedRoute>? availableRoutes,
    AssignedRoute? selectedRoute,
    bool clearSelectedRoute = false,
    int? monthOffset,
    List<OutletBillingSummary>? outletSummaries,
    double? grandTotal,
    int? totalBillingCount,
    bool? loadingOutlets,
  }) {
    return OutletBillingsLoaded(
      availableRoutes: availableRoutes ?? this.availableRoutes,
      selectedRoute: clearSelectedRoute ? null : (selectedRoute ?? this.selectedRoute),
      monthOffset: monthOffset ?? this.monthOffset,
      outletSummaries: outletSummaries ?? this.outletSummaries,
      grandTotal: grandTotal ?? this.grandTotal,
      totalBillingCount: totalBillingCount ?? this.totalBillingCount,
      loadingOutlets: loadingOutlets ?? this.loadingOutlets,
    );
  }
}

class OutletBillingsError extends OutletBillingsState {
  final String message;
  const OutletBillingsError(this.message);
}
