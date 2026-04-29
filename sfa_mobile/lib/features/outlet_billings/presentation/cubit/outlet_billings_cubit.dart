import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:uswatte/features/outlet_billings/domain/entities/assigned_route.dart';
import 'package:uswatte/features/outlet_billings/domain/usecases/get_assigned_routes_usecase.dart';
import 'package:uswatte/features/outlet_billings/domain/usecases/get_outlet_summary_usecase.dart';
import 'package:uswatte/features/outlet_billings/presentation/cubit/outlet_billings_state.dart';

class OutletBillingsCubit extends Cubit<OutletBillingsState> {
  final GetAssignedRoutesUseCase _getAssignedRoutes;
  final GetOutletSummaryUseCase _getOutletSummary;

  // All raw assignments fetched once — includes dates so we can filter per month
  List<AssignedRoute> _allAssignments = [];

  OutletBillingsCubit(this._getAssignedRoutes, this._getOutletSummary)
      : super(const OutletBillingsInitial());

  Future<void> load() async {
    emit(const OutletBillingsRoutesLoading());
    try {
      _allAssignments = await _getAssignedRoutes.call();
      emit(OutletBillingsLoaded(
        availableRoutes: _routesForMonth(0),
        monthOffset: 0,
      ));
    } catch (e) {
      emit(OutletBillingsError(e.toString()));
    }
  }

  void changeMonth(int offset) {
    final current = state;
    if (current is! OutletBillingsLoaded) return;
    emit(OutletBillingsLoaded(
      availableRoutes: _routesForMonth(offset),
      monthOffset: offset,
    ));
  }

  Future<void> selectRoute(AssignedRoute route) async {
    final current = state;
    if (current is! OutletBillingsLoaded) return;

    emit(current.copyWith(
      selectedRoute: route,
      loadingOutlets: true,
      outletSummaries: [],
      grandTotal: 0,
      totalBillingCount: 0,
    ));

    try {
      final start = _monthStart(current.monthOffset);
      final end = _monthEnd(current.monthOffset);

      final summaries = await _getOutletSummary.call(
        routeId: route.routeId,
        dateFrom: _formatDate(start),
        dateTo: _formatDate(end),
      );

      final grandTotal = summaries.fold(0.0, (sum, s) => sum + s.totalAmount);
      final totalCount = summaries.fold(0, (sum, s) => sum + s.billingCount);

      emit(current.copyWith(
        selectedRoute: route,
        outletSummaries: summaries,
        grandTotal: grandTotal,
        totalBillingCount: totalCount,
        loadingOutlets: false,
      ));
    } catch (e) {
      final s = state;
      if (s is OutletBillingsLoaded) {
        emit(s.copyWith(loadingOutlets: false));
      }
      emit(OutletBillingsError(e.toString()));
    }
  }

  /// Filters all cached assignments to the given month offset, then deduplicates
  /// by routeId so each route appears once regardless of how many days it was active.
  List<AssignedRoute> _routesForMonth(int offset) {
    final start = _monthStart(offset);
    final end = _monthEnd(offset);
    final seen = <int>{};
    return _allAssignments
        .where((a) =>
            !a.assignedDate.isBefore(start) && !a.assignedDate.isAfter(end))
        .where((a) => seen.add(a.routeId))
        .toList()
      ..sort((a, b) => a.routeName.compareTo(b.routeName));
  }

  DateTime _monthStart(int offset) {
    final now = DateTime.now();
    return DateTime(now.year, now.month - offset, 1);
  }

  DateTime _monthEnd(int offset) {
    final s = _monthStart(offset);
    return DateTime(s.year, s.month + 1, 0);
  }

  String _formatDate(DateTime d) {
    final m = d.month.toString().padLeft(2, '0');
    final day = d.day.toString().padLeft(2, '0');
    return '${d.year}-$m-$day';
  }

  String monthLabel(int offset) {
    const months = [
      'Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun',
      'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'
    ];
    final d = _monthStart(offset);
    return '${months[d.month - 1]} ${d.year}';
  }
}
