import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:uswatte/core/errors/app_exception.dart';
import 'package:uswatte/features/outlets/domain/usecases/get_current_route_id_usecase.dart';
import 'package:uswatte/features/outlets/domain/usecases/get_outlets_usecase.dart';
import 'package:uswatte/features/outlets/domain/usecases/sync_outlets_usecase.dart';
import 'package:uswatte/features/outlets/presentation/bloc/outlets_event.dart';
import 'package:uswatte/features/outlets/presentation/bloc/outlets_state.dart';

class OutletsBloc extends Bloc<OutletsEvent, OutletsState> {
  final GetOutletsUseCase _getOutlets;
  final SyncOutletsUseCase _syncOutlets;
  final GetCurrentRouteIdUseCase _getCurrentRouteId;

  OutletsBloc({
    required GetOutletsUseCase getOutletsUseCase,
    required SyncOutletsUseCase syncOutletsUseCase,
    required GetCurrentRouteIdUseCase getCurrentRouteIdUseCase,
  })  : _getOutlets = getOutletsUseCase,
        _syncOutlets = syncOutletsUseCase,
        _getCurrentRouteId = getCurrentRouteIdUseCase,
        super(const OutletsInitial()) {
    on<LoadOutletsRequested>(_onLoad);
    on<SyncDailyOutletsRequested>(_onSync);
  }

  Future<void> _onLoad(
    LoadOutletsRequested event,
    Emitter<OutletsState> emit,
  ) async {
    // Preserve assignment status — only _onSync (fired by home page) knows
    // whether today has a real assignment. _onLoad must not overwrite it.
    final wasAssigned =
        state is OutletsLoaded && (state as OutletsLoaded).hasActiveAssignment;

    emit(const OutletsLoading());
    try {
      final local = await _getOutlets();
      final routeId = await _getCurrentRouteId();

      // No stored route or no local data — wait for the assignment to trigger sync
      if (routeId == null || local.isEmpty) {
        emit(OutletsLoaded(
            outlets: local,
            isSyncing: false,
            hasActiveAssignment: wasAssigned));
        return;
      }

      // Use the cached routeName from local data for the background sync
      final routeName = local.first.routeName;
      emit(OutletsLoaded(
          outlets: local, isSyncing: true, hasActiveAssignment: wasAssigned));

      final synced = await _syncOutlets(routeId, routeName);
      emit(OutletsLoaded(
        outlets: synced,
        isSyncing: false,
        lastSyncedAt: DateTime.now(),
        hasActiveAssignment: wasAssigned,
      ));
    } on AppException catch (e) {
      final current = state;
      if (current is OutletsLoaded) {
        emit(current.copyWith(isSyncing: false));
      } else {
        emit(OutletsError(message: e.message));
      }
    }
  }

  Future<void> _onSync(
    SyncDailyOutletsRequested event,
    Emitter<OutletsState> emit,
  ) async {
    final current = state;
    if (current is OutletsLoaded) {
      emit(current.copyWith(isSyncing: true));
    }

    try {
      final synced = await _syncOutlets(event.routeId, event.routeName);
      // hasActiveAssignment: true — this event is only ever fired from the home
      // page's AssignmentsBloc listener when today's assignment actually exists.
      emit(OutletsLoaded(
        outlets: synced,
        isSyncing: false,
        lastSyncedAt: DateTime.now(),
        hasActiveAssignment: true,
      ));
    } on AppException catch (e) {
      final prev = state;
      if (prev is OutletsLoaded) {
        emit(prev.copyWith(isSyncing: false));
      } else {
        emit(OutletsError(message: e.message));
      }
    }
  }
}
