import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:geolocator/geolocator.dart';
import 'package:uswatte/features/todays_route_map/domain/usecases/get_todays_route_map_usecase.dart';
import 'package:uswatte/features/todays_route_map/presentation/bloc/todays_route_map_event.dart';
import 'package:uswatte/features/todays_route_map/presentation/bloc/todays_route_map_state.dart';

class TodaysRouteMapBloc
    extends Bloc<TodaysRouteMapEvent, TodaysRouteMapState> {
  final GetTodaysRouteMapUseCase _useCase;

  TodaysRouteMapBloc(this._useCase) : super(const TodaysRouteMapInitial()) {
    on<LoadTodaysRouteMapRequested>(_onLoad);
  }

  Future<void> _onLoad(
    LoadTodaysRouteMapRequested event,
    Emitter<TodaysRouteMapState> emit,
  ) async {
    emit(const TodaysRouteMapLoading());
    try {
      final result = await _useCase();
      final position = await _getPosition();
      emit(TodaysRouteMapLoaded(
        outlets: result.outlets,
        userPosition: position,
        lastBilledOutletId: result.lastBilledOutletId,
      ));
    } catch (e) {
      emit(TodaysRouteMapError(e.toString()));
    }
  }

  Future<Position?> _getPosition() async {
    try {
      final serviceEnabled = await Geolocator.isLocationServiceEnabled();
      if (!serviceEnabled) return null;

      var permission = await Geolocator.checkPermission();
      if (permission == LocationPermission.denied) {
        permission = await Geolocator.requestPermission();
      }
      if (permission == LocationPermission.denied ||
          permission == LocationPermission.deniedForever) {
        return null;
      }

      return await Geolocator.getCurrentPosition(
        locationSettings: const LocationSettings(accuracy: LocationAccuracy.high),
      );
    } catch (_) {
      return null;
    }
  }
}
