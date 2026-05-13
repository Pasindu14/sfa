import 'package:uswatte/features/stock/data/datasources/distributor_stock_local_datasource.dart';
import 'package:uswatte/features/stock/data/datasources/distributor_stock_remote_datasource.dart';

class SyncDistributorStockUseCase {
  final DistributorStockRemoteDatasource _remote;
  final DistributorStockLocalDatasource _local;

  const SyncDistributorStockUseCase(this._remote, this._local);

  Future<void> call() async {
    final stocks = await _remote.fetchAll();
    await _local.replaceAll(stocks);
    await _local.saveLastSyncedAt(DateTime.now());
  }
}
