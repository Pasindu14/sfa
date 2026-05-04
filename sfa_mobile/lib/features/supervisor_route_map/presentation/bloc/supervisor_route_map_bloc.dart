import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:uswatte/features/route_assignment/domain/usecases/get_my_reps_usecase.dart';
import 'package:uswatte/features/supervisor_route_map/domain/usecases/get_supervisor_route_map_usecase.dart';
import 'package:uswatte/features/supervisor_route_map/presentation/bloc/supervisor_route_map_event.dart';
import 'package:uswatte/features/supervisor_route_map/presentation/bloc/supervisor_route_map_state.dart';

class SupervisorRouteMapBloc
    extends Bloc<SupervisorRouteMapEvent, SupervisorRouteMapState> {
  final GetMyRepsUseCase _getMyReps;
  final GetSupervisorRouteMapUseCase _getRouteMap;

  SupervisorRouteMapBloc({
    required GetMyRepsUseCase getMyReps,
    required GetSupervisorRouteMapUseCase getRouteMap,
  })  : _getMyReps = getMyReps,
        _getRouteMap = getRouteMap,
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
      final outlets = await _getRouteMap(ready.selectedRep!.userId, DateTime.now());
      emit(SupervisorRouteMapLoaded(
        outlets: outlets,
        rep: ready.selectedRep!,
      ));
    } catch (e) {
      emit(ready.copyWith(isLoadingMap: false, mapError: e.toString()));
    }
  }

  Future<void> _onRefresh(
    SupervisorRouteMapRefreshRequested event,
    Emitter<SupervisorRouteMapState> emit,
  ) async {
    if (state is! SupervisorRouteMapLoaded) return;
    final loaded = state as SupervisorRouteMapLoaded;
    try {
      final outlets = await _getRouteMap(loaded.rep.userId, DateTime.now());
      emit(SupervisorRouteMapLoaded(outlets: outlets, rep: loaded.rep));
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
