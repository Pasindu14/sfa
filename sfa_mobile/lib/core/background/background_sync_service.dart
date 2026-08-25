import 'package:flutter/foundation.dart';
import 'package:uswatte/core/background/location_tracking_service.dart';
import 'package:uswatte/core/sync/bill_sync_service.dart';
import 'package:uswatte/core/sync/not_billing_sync_service.dart';
import 'package:uswatte/features/outlets/data/datasources/outlets_local_datasource.dart';
import 'package:uswatte/features/outlets/domain/usecases/sync_outlets_usecase.dart';
import 'package:uswatte/features/products/domain/usecases/sync_product_categories_usecase.dart';
import 'package:uswatte/features/products/domain/usecases/sync_products_usecase.dart';
import 'package:uswatte/features/route_assignment/domain/usecases/get_assignments_usecase.dart';
import 'package:uswatte/features/stock/domain/usecases/sync_distributor_stock_usecase.dart';

/// Rep-facing progress of a full sync.
///
/// Exposed as a [ValueNotifier] rather than a broadcast stream because the
/// Home header mounts *after* the post-login sync starts — login redirects
/// straight to Home — and a broadcast stream would leave that late listener
/// with nothing to render until the next step boundary.
class AppSyncProgress {
  /// True while [BackgroundSyncService.runSync] is working.
  final bool isSyncing;

  /// Short label for the step in flight, e.g. 'Products'. Null when idle.
  final String? step;

  /// When the last run finished. Null until one completes.
  final DateTime? completedAt;

  const AppSyncProgress._({
    required this.isSyncing,
    this.step,
    this.completedAt,
  });

  const AppSyncProgress.idle() : this._(isSyncing: false);

  const AppSyncProgress.running(String step)
      : this._(isSyncing: true, step: step);

  AppSyncProgress.done(DateTime at) : this._(isSyncing: false, completedAt: at);
}

class BackgroundSyncService {
  final SyncProductsUseCase _syncProducts;
  final SyncProductCategoriesUseCase _syncCategories;
  final SyncOutletsUseCase _syncOutlets;
  final SyncDistributorStockUseCase _syncStock;
  final GetAssignmentsUseCase _getAssignments;
  final BillSyncService _billSync;
  final NotBillingSyncService _notBillingSync;
  final OutletsLocalDatasource _outletsLocal;

  BackgroundSyncService({
    required SyncProductsUseCase syncProducts,
    required SyncProductCategoriesUseCase syncCategories,
    required SyncOutletsUseCase syncOutlets,
    required SyncDistributorStockUseCase syncStock,
    required GetAssignmentsUseCase getAssignments,
    required BillSyncService billSync,
    required NotBillingSyncService notBillingSync,
    required OutletsLocalDatasource outletsLocal,
  })  : _syncProducts = syncProducts,
        _syncCategories = syncCategories,
        _syncOutlets = syncOutlets,
        _syncStock = syncStock,
        _getAssignments = getAssignments,
        _billSync = billSync,
        _notBillingSync = notBillingSync,
        _outletsLocal = outletsLocal;

  /// Live progress for the UI. Never replaced — listeners attach once.
  final ValueNotifier<AppSyncProgress> progress =
      ValueNotifier<AppSyncProgress>(const AppSyncProgress.idle());

  /// Runs all sync steps sequentially. Each step is individually guarded so
  /// one failure never blocks the rest. Always returns true — WorkManager
  /// interprets a false/exception return as a signal to retry immediately,
  /// which is undesirable for a periodic background task.
  ///
  /// [refreshRouteAssignment] re-fetches today's route assignment from the
  /// server before syncing outlets, instead of trusting the route id already
  /// stored on the device. Used after login, where the rep may have been
  /// assigned a different route (or their first route ever) since last run.
  /// The periodic background task leaves it off — it runs unattended and the
  /// stored route is good enough there.
  Future<bool> runSync({bool refreshRouteAssignment = false}) async {
    progress.value = const AppSyncProgress.running('Products');
    try {
      await _syncProducts();
    } catch (_) {}

    try {
      await _syncCategories();
    } catch (_) {}

    progress.value = const AppSyncProgress.running('Outlets');

    try {
      int? routeId;
      String? routeName;

      if (refreshRouteAssignment) {
        try {
          final result = await _getAssignments(date: DateTime.now());
          final assignment =
              result.assignments.isEmpty ? null : result.assignments.first;
          routeId = assignment?.routeId;
          routeName = assignment?.routeName;
        } catch (_) {}
      }

      // Fall back to the route already on the device when the assignment
      // fetch was skipped or failed — better a stale route than none.
      routeId ??= await _outletsLocal.getCurrentRouteId();
      routeName ??= await _outletsLocal.getCurrentRouteName();

      if (routeId != null && routeName != null) {
        await _syncOutlets(routeId, routeName);
      }
    } catch (_) {}

    progress.value = const AppSyncProgress.running('Stock');
    try {
      await _syncStock();
    } catch (_) {}

    progress.value = const AppSyncProgress.running('Uploading');
    try {
      await _billSync.flushAll();
    } catch (_) {}

    try {
      await _notBillingSync.flushAll();
    } catch (_) {}

    // Backstop flush for any pings queued while the foreground service was offline.
    try {
      await flushLocationPingQueue();
    } catch (_) {}

    progress.value = AppSyncProgress.done(DateTime.now());
    return true;
  }
}
