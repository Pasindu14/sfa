import 'package:bloc_concurrency/bloc_concurrency.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:uswatte/core/errors/app_exception.dart';
import 'package:uswatte/features/outlets/domain/usecases/get_current_route_id_usecase.dart';
import 'package:uswatte/features/outlets/domain/usecases/get_outlets_last_synced_at_usecase.dart';
import 'package:uswatte/features/outlets/domain/usecases/get_outlets_usecase.dart';
import 'package:uswatte/features/outlets/domain/usecases/sync_outlets_usecase.dart';
import 'package:uswatte/features/outlets/presentation/bloc/outlets_event.dart';
import 'package:uswatte/features/outlets/presentation/bloc/outlets_state.dart';

class OutletsBloc extends Bloc<OutletsEvent, OutletsState> {
  final GetOutletsUseCase _getOutlets;
  final SyncOutletsUseCase _syncOutlets;
  final GetCurrentRouteIdUseCase _getCurrentRouteId;
  final GetOutletsLastSyncedAtUseCase _getLastSyncedAt;

  OutletsBloc({
    required GetOutletsUseCase getOutletsUseCase,
    required SyncOutletsUseCase syncOutletsUseCase,
    required GetCurrentRouteIdUseCase getCurrentRouteIdUseCase,
    required GetOutletsLastSyncedAtUseCase getOutletsLastSyncedAtUseCase,
  })  : _getOutlets = getOutletsUseCase,
        _syncOutlets = syncOutletsUseCase,
        _getCurrentRouteId = getCurrentRouteIdUseCase,
        _getLastSyncedAt = getOutletsLastSyncedAtUseCase,
        super(const OutletsInitial()) {
    on<LoadOutletsRequested>(_onLoad, transformer: sequential());
    on<SyncDailyOutletsRequested>(_onSync, transformer: sequential());
  }

  Future<void> _onLoad(
    LoadOutletsRequested event,
    Emitter<OutletsState> emit,
  ) async {
    // Only reads from local SQLite — never writes current_route_id.
    // All syncing (and route metadata updates) is done exclusively in _onSync,
    // which receives the authoritative routeId from the server assignment.
    final wasAssigned = state is OutletsLoaded
        ? (state as OutletsLoaded).hasActiveAssignment
        : null;

    emit(const OutletsLoading());
    try {
      final local = await _getOutlets();
      final routeId = await _getCurrentRouteId();
      final lastSyncedAt = await _getLastSyncedAt();
      final hasAssignment = wasAssigned ?? (local.isNotEmpty && routeId != null);
      emit(OutletsLoaded(
        outlets: local,
        isSyncing: false,
        lastSyncedAt: lastSyncedAt,
        hasActiveAssignment: hasAssignment,
      ));
    } on AppException catch (e) {
      emit(OutletsError(message: e.message));
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
