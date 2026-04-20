import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:uswatte/core/errors/app_exception.dart';
import 'package:uswatte/features/products/domain/usecases/get_products_usecase.dart';
import 'package:uswatte/features/products/domain/usecases/sync_products_usecase.dart';
import 'package:uswatte/features/products/presentation/bloc/products_event.dart';
import 'package:uswatte/features/products/presentation/bloc/products_state.dart';

class ProductsBloc extends Bloc<ProductsEvent, ProductsState> {
  final GetProductsUseCase _getProducts;
  final SyncProductsUseCase _syncProducts;

  ProductsBloc({
    required GetProductsUseCase getProductsUseCase,
    required SyncProductsUseCase syncProductsUseCase,
  })  : _getProducts = getProductsUseCase,
        _syncProducts = syncProductsUseCase,
        super(const ProductsInitial()) {
    on<LoadProductsRequested>(_onLoad);
    on<SyncProductsRequested>(_onSync);
  }

  Future<void> _onLoad(
    LoadProductsRequested event,
    Emitter<ProductsState> emit,
  ) async {
    emit(const ProductsLoading());
    try {
      // 1. Serve local data immediately — works fully offline
      final local = await _getProducts();
      emit(ProductsLoaded(products: local, isSyncing: true));

      // 2. Background sync — refreshes from API and updates list
      final (synced, cachedAt) = await _syncProducts();
      emit(ProductsLoaded(
          products: synced, isSyncing: false, lastSyncedAt: cachedAt));
    } on AppException catch (e) {
      // If we have local data, keep showing it — don't blank the screen on
      // a network failure. Only show error state when there's nothing local.
      final current = state;
      if (current is ProductsLoaded) {
        emit(current.copyWith(isSyncing: false));
      } else {
        emit(ProductsError(message: e.message));
      }
    }
  }

  Future<void> _onSync(
    SyncProductsRequested event,
    Emitter<ProductsState> emit,
  ) async {
    final current = state;
    if (current is! ProductsLoaded) return;

    emit(current.copyWith(isSyncing: true));
    try {
      final (synced, cachedAt) = await _syncProducts();
      emit(ProductsLoaded(
          products: synced, isSyncing: false, lastSyncedAt: cachedAt));
    } on AppException {
      // Restore the previous loaded state — sync failure shouldn't clear the list
      emit(current.copyWith(isSyncing: false));
    }
  }
}
