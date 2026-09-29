import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:uswatte/features/route_assignment/domain/usecases/get_my_reps_usecase.dart';
import 'package:uswatte/features/route_assignment/domain/entities/rep_summary.dart';
import 'package:uswatte/features/supervisor_route_map/domain/entities/rep_last_location.dart';
import 'package:uswatte/features/supervisor_route_map/domain/usecases/get_rep_last_location_usecase.dart';
import 'package:uswatte/features/supervisor_route_map/domain/usecases/get_supervisor_route_map_usecase.dart';
import 'package:uswatte/features/supervisor_route_map/presentation/bloc/supervisor_route_map_event.dart';
import 'package:uswatte/features/supervisor_route_map/presentation/bloc/supervisor_route_map_state.dart';

class SupervisorRouteMapBloc
    extends Bloc<SupervisorRouteMapEvent, SupervisorRouteMapState> {
  final GetMyRepsUseCase _getMyReps;
  final GetSupervisorRouteMapUseCase _getRouteMap;
  final GetRepLastLocationUseCase? _getLastLocation;

  SupervisorRouteMapBloc({
    required GetMyRepsUseCase getMyReps,
    required GetSupervisorRouteMapUseCase getRouteMap,
    GetRepLastLocationUseCase? getLastLocation,
  })  : _getMyReps = getMyReps,
        _getRouteMap = getRouteMap,
        _getLastLocation = getLastLocation,
        super(const SupervisorRouteMapInitial()) {
    on<SupervisorRouteMapRepsRequested>(_onLoadReps);
    on<SupervisorRouteMapRepSelected>(_onRepSelected);
    on<SupervisorRouteMapLoadRequested>(_onLoadMap);
    on<SupervisorRouteMapRefreshRequested>(_onRefresh);
    on<SupervisorRouteMapBackToSelectorRequested>(_onBackToSelector);
  }

  Future<void> _onLoadReps(
    SupervisorRouteMapRepsRequested event,
    Emitter<SupervisorRouteMapState> emit,
  ) async {
    emit(const SupervisorRouteMapLoadingReps());
    try {
      final reps = await _getMyReps();
      emit(SupervisorRouteMapReady(reps: reps));
    } catch (e) {
      emit(SupervisorRouteMapRepsError(e.toString()));
    }
  }

  void _onRepSelected(
    SupervisorRouteMapRepSelected event,
    Emitter<SupervisorRouteMapState> emit,
  ) {
    if (state is SupervisorRouteMapReady) {
      emit((state as SupervisorRouteMapReady).copyWith(
        selectedRep: event.rep,
        clearMapError: true,
      ));
    }
  }

  Future<void> _onLoadMap(
    SupervisorRouteMapLoadRequested event,
    Emitter<SupervisorRouteMapState> emit,
  ) async {
    final ready = state as SupervisorRouteMapReady;
    if (ready.selectedRep == null) return;

    emit(ready.copyWith(isLoadingMap: true, clearMapError: true));
    try {
      emit(await _load(ready.selectedRep!));
    } catch (e) {
      emit(ready.copyWith(isLoadingMap: false, mapError: e.toString()));
    }
  }

  /// Outlets and the last location load in parallel. The location is
  /// best-effort: its failure never costs the supervisor the outlet map.
  Future<SupervisorRouteMapLoaded> _load(RepSummary rep) async {
    final locationFuture = _fetchLastLocation(rep.userId);
    final outlets = await _getRouteMap(rep.userId, DateTime.now());
    final (location, failed) = await locationFuture;
    return SupervisorRouteMapLoaded(
      outlets: outlets,
      rep: rep,
      lastLocation: location,
      lastLocationFailed: failed,
    );
  }

  Future<(RepLastLocation?, bool)> _fetchLastLocation(int userId) async {
    final useCase = _getLastLocation;
    if (useCase == null) return (null, false);
    try {
      return (await useCase(userId), false);
    } catch (_) {
      return (null, true);
    }
  }

  Future<void> _onRefresh(
    SupervisorRouteMapRefreshRequested event,
    Emitter<SupervisorRouteMapState> emit,
  ) async {
    if (state is! SupervisorRouteMapLoaded) return;
    final loaded = state as SupervisorRouteMapLoaded;
    try {
      emit(await _load(loaded.rep));
    } catch (_) {
      // keep current map on refresh failure
    }
  }

  Future<void> _onBackToSelector(
    SupervisorRouteMapBackToSelectorRequested event,
    Emitter<SupervisorRouteMapState> emit,
  ) async {
    if (state is SupervisorRouteMapLoaded) {
      final loaded = state as SupervisorRouteMapLoaded;
      emit(const SupervisorRouteMapLoadingReps());
      try {
        final reps = await _getMyReps();
        final match =
            reps.where((r) => r.userId == loaded.rep.userId).firstOrNull;
        emit(SupervisorRouteMapReady(reps: reps, selectedRep: match));
      } catch (e) {
        emit(SupervisorRouteMapRepsError(e.toString()));
      }
    }
  }

}
