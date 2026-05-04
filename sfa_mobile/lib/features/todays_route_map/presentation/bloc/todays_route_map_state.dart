import 'package:equatable/equatable.dart';
import 'package:geolocator/geolocator.dart';
import 'package:uswatte/features/todays_route_map/domain/entities/route_map_outlet.dart';

abstract class TodaysRouteMapState extends Equatable {
  const TodaysRouteMapState();
  @override
  List<Object?> get props => [];
}

class TodaysRouteMapInitial extends TodaysRouteMapState {
  const TodaysRouteMapInitial();
}

class TodaysRouteMapLoading extends TodaysRouteMapState {
  const TodaysRouteMapLoading();
}

class TodaysRouteMapLoaded extends TodaysRouteMapState {
  final List<RouteMapOutlet> outlets;
  final Position? userPosition;
  final int? lastBilledOutletId;

  const TodaysRouteMapLoaded({
    required this.outlets,
    this.userPosition,
    this.lastBilledOutletId,
  });

  @override
  List<Object?> get props => [outlets, userPosition, lastBilledOutletId];
}

class TodaysRouteMapError extends TodaysRouteMapState {
  final String message;
  const TodaysRouteMapError(this.message);

  @override
  List<Object?> get props => [message];
}
