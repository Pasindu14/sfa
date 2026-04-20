import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:uswatte/core/errors/app_exception.dart';
import 'package:uswatte/features/pricing/domain/usecases/get_pricing_usecase.dart';
import 'package:uswatte/features/pricing/domain/usecases/sync_pricing_usecase.dart';
import 'package:uswatte/features/pricing/presentation/bloc/pricing_event.dart';
import 'package:uswatte/features/pricing/presentation/bloc/pricing_state.dart';

class PricingBloc extends Bloc<PricingEvent, PricingState> {
  final GetPricingUseCase _getPricing;
  final SyncPricingUseCase _syncPricing;

  PricingBloc({
    required GetPricingUseCase getPricingUseCase,
    required SyncPricingUseCase syncPricingUseCase,
  })  : _getPricing = getPricingUseCase,
        _syncPricing = syncPricingUseCase,
        super(const PricingInitial()) {
    on<LoadPricingRequested>(_onLoad);
    on<SyncPricingRequested>(_onSync);
  }

  Future<void> _onLoad(
    LoadPricingRequested event,
    Emitter<PricingState> emit,
  ) async {
    emit(const PricingLoading());
    try {
      final local = await _getPricing();
      emit(PricingLoaded(structures: local, isSyncing: true));

      final (synced, syncedAt) = await _syncPricing();
      emit(PricingLoaded(
          structures: synced, isSyncing: false, lastSyncedAt: syncedAt));
    } on AppException catch (e) {
      final current = state;
      if (current is PricingLoaded) {
        emit(current.copyWith(isSyncing: false));
      } else {
        emit(PricingError(message: e.message));
      }
    }
  }

  Future<void> _onSync(
    SyncPricingRequested event,
    Emitter<PricingState> emit,
  ) async {
    final current = state;
    if (current is PricingLoaded) {
      emit(current.copyWith(isSyncing: true));
    }

    try {
      final (synced, syncedAt) = await _syncPricing();
      emit(PricingLoaded(
          structures: synced, isSyncing: false, lastSyncedAt: syncedAt));
    } on AppException catch (e) {
      final prev = state;
      if (prev is PricingLoaded) {
        emit(prev.copyWith(isSyncing: false));
      } else {
        emit(PricingError(message: e.message));
      }
    }
  }
}
