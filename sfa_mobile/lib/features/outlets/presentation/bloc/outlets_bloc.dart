import 'package:bloc_concurrency/bloc_concurrency.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:uswatte/core/constants/app_constants.dart';
import 'package:uswatte/core/errors/app_exception.dart';
import 'package:uswatte/features/outlets/domain/entities/outlet.dart';
import 'package:uswatte/features/outlets/domain/usecases/get_current_route_id_usecase.dart';
import 'package:uswatte/features/outlets/domain/usecases/get_geofence_radius_usecase.dart';
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
  final GetGeofenceRadiusUseCase _getGeofenceRadius;

  OutletsBloc({
    required GetOutletsUseCase getOutletsUseCase,
    required SyncOutletsUseCase syncOutletsUseCase,
    required GetCurrentRouteIdUseCase getCurrentRouteIdUseCase,
    required GetOutletsLastSyncedAtUseCase getOutletsLastSyncedAtUseCase,
    required GetGeofenceRadiusUseCase getGeofenceRadiusUseCase,
  })  : _getOutlets = getOutletsUseCase,
        _syncOutlets = syncOutletsUseCase,
        _getCurrentRouteId = getCurrentRouteIdUseCase,
        _getLastSyncedAt = getOutletsLastSyncedAtUseCase,
        _getGeofenceRadius = getGeofenceRadiusUseCase,
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
      final storedRadius = await _getGeofenceRadius();
      final radiusMeters =
          storedRadius ?? AppConstants.billingProximityRadiusMeters;

      // The daily_outlets table is a full-replace snapshot keyed to a sync date.
      // If the last sync was on a previous calendar day, the rows are stale —
      // show nothing until today's SyncDailyOutletsRequested runs from the home page.
      final todayOutlets = _isSyncedToday(lastSyncedAt) ? local : const <Outlet>[];
      final hasAssignment =
          wasAssigned ?? (todayOutlets.isNotEmpty && routeId != null);

      emit(OutletsLoaded(
        outlets: todayOutlets,
        isSyncing: false,
        lastSyncedAt: lastSyncedAt,
        hasActiveAssignment: hasAssignment,
        geofenceRadiusMeters: radiusMeters,
      ));
    } on AppException catch (e) {
      emit(OutletsError(message: e.message));
    }
  }

  bool _isSyncedToday(DateTime? lastSyncedAt) {
    if (lastSyncedAt == null) return false;
    final now = DateTime.now();
    return lastSyncedAt.year == now.year &&
        lastSyncedAt.month == now.month &&
        lastSyncedAt.day == now.day;
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
        outlets: synced.outlets,
        isSyncing: false,
        lastSyncedAt: DateTime.now(),
        hasActiveAssignment: true,
        geofenceRadiusMeters: synced.geofenceRadiusMeters,
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
