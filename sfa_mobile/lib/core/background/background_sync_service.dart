import 'package:uswatte/core/background/location_tracking_service.dart';
import 'package:uswatte/core/sync/bill_sync_service.dart';
import 'package:uswatte/core/sync/not_billing_sync_service.dart';
import 'package:uswatte/features/outlets/data/datasources/outlets_local_datasource.dart';
import 'package:uswatte/features/outlets/domain/usecases/sync_outlets_usecase.dart';
import 'package:uswatte/features/products/domain/usecases/sync_product_categories_usecase.dart';
import 'package:uswatte/features/products/domain/usecases/sync_products_usecase.dart';
import 'package:uswatte/features/stock/domain/usecases/sync_distributor_stock_usecase.dart';

class BackgroundSyncService {
  final SyncProductsUseCase _syncProducts;
  final SyncProductCategoriesUseCase _syncCategories;
  final SyncOutletsUseCase _syncOutlets;
  final SyncDistributorStockUseCase _syncStock;
  final BillSyncService _billSync;
  final NotBillingSyncService _notBillingSync;
  final OutletsLocalDatasource _outletsLocal;

  const BackgroundSyncService({
    required SyncProductsUseCase syncProducts,
    required SyncProductCategoriesUseCase syncCategories,
    required SyncOutletsUseCase syncOutlets,
    required SyncDistributorStockUseCase syncStock,
    required BillSyncService billSync,
    required NotBillingSyncService notBillingSync,
    required OutletsLocalDatasource outletsLocal,
  })  : _syncProducts = syncProducts,
        _syncCategories = syncCategories,
        _syncOutlets = syncOutlets,
        _syncStock = syncStock,
        _billSync = billSync,
        _notBillingSync = notBillingSync,
        _outletsLocal = outletsLocal;

  /// Runs all sync steps sequentially. Each step is individually guarded so
  /// one failure never blocks the rest. Always returns true — WorkManager
  /// interprets a false/exception return as a signal to retry immediately,
  /// which is undesirable for a periodic background task.
  Future<bool> runSync() async {
    try {
      await _syncProducts();
    } catch (_) {}

    try {
      await _syncCategories();
    } catch (_) {}

    try {
      final routeId = await _outletsLocal.getCurrentRouteId();
      final routeName = await _outletsLocal.getCurrentRouteName();
      if (routeId != null && routeName != null) {
        await _syncOutlets(routeId, routeName);
      }
    } catch (_) {}

    try {
      await _syncStock();
    } catch (_) {}

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

    return true;
  }
}
