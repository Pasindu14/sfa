import 'package:bloc_concurrency/bloc_concurrency.dart';
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
    // Sequential transformer ensures _onLoad and _onSync never run at the same
    // time. If both fire on page open, _onLoad finishes first, then _onSync
    // runs last and always wins with the correct fresh routeId.
    on<LoadOutletsRequested>(_onLoad, transformer: sequential());
    on<SyncDailyOutletsRequested>(_onSync, transformer: sequential());
  }

  Future<void> _onLoad(
    LoadOutletsRequested event,
    Emitter<OutletsState> emit,
  ) async {
    // If _onSync already ran in this instance, preserve its assignment status.
    // For fresh instances (e.g. create-bill page's own bloc), derive the flag
    // from stored data: outlets in SQLite + a saved routeId means a real sync
    // happened at some point with a valid assignment.
    final wasAssigned = state is OutletsLoaded
        ? (state as OutletsLoaded).hasActiveAssignment
        : null; // null = unknown, will be derived below

    emit(const OutletsLoading());
    try {
      final local = await _getOutlets();
      final routeId = await _getCurrentRouteId();

      // Derive hasActiveAssignment: trust _onSync result if available,
      // otherwise infer from whether a prior sync left data in SQLite.
      final hasAssignment = wasAssigned ?? (local.isNotEmpty && routeId != null);

      // No stored route or no local data — nothing to sync yet
      if (routeId == null || local.isEmpty) {
        emit(OutletsLoaded(
            outlets: local,
            isSyncing: false,
            hasActiveAssignment: hasAssignment));
        return;
      }

      // Use the cached routeName from local data for the background sync
      final routeName = local.first.routeName;
      emit(OutletsLoaded(
          outlets: local, isSyncing: true, hasActiveAssignment: hasAssignment));

      final synced = await _syncOutlets(routeId, routeName);
      emit(OutletsLoaded(
        outlets: synced,
        isSyncing: false,
        lastSyncedAt: DateTime.now(),
        hasActiveAssignment: hasAssignment,
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
