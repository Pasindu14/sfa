import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:uswatte/core/errors/app_exception.dart';
import 'package:uswatte/features/products/domain/usecases/get_product_categories_usecase.dart';
import 'package:uswatte/features/products/domain/usecases/sync_product_categories_usecase.dart';
import 'package:uswatte/features/products/presentation/bloc/product_categories_event.dart';
import 'package:uswatte/features/products/presentation/bloc/product_categories_state.dart';

class ProductCategoriesBloc
    extends Bloc<ProductCategoriesEvent, ProductCategoriesState> {
  final GetProductCategoriesUseCase _getCategories;
  final SyncProductCategoriesUseCase _syncCategories;

  ProductCategoriesBloc({
    required GetProductCategoriesUseCase getProductCategoriesUseCase,
    required SyncProductCategoriesUseCase syncProductCategoriesUseCase,
  })  : _getCategories = getProductCategoriesUseCase,
        _syncCategories = syncProductCategoriesUseCase,
        super(const ProductCategoriesInitial()) {
    on<LoadProductCategoriesRequested>(_onLoad);
  }

  Future<void> _onLoad(
    LoadProductCategoriesRequested event,
    Emitter<ProductCategoriesState> emit,
  ) async {
    emit(const ProductCategoriesLoading());
    try {
      // 1. Serve local cache immediately — works fully offline
      final local = await _getCategories();
      emit(ProductCategoriesLoaded(categories: local, isSyncing: true));

      // 2. Background sync — refreshes from API and updates list
      final (synced, cachedAt) = await _syncCategories();
      emit(ProductCategoriesLoaded(
          categories: synced, isSyncing: false, lastSyncedAt: cachedAt));
    } on AppException catch (e) {
      final current = state;
      if (current is ProductCategoriesLoaded) {
        emit(current.copyWith(isSyncing: false));
      } else {
        emit(ProductCategoriesError(message: e.message));
      }
    }
  }
}
