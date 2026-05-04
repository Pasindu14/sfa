import 'package:equatable/equatable.dart';
import 'package:uswatte/features/route_assignment/domain/entities/rep_summary.dart';
import 'package:uswatte/features/todays_route_map/domain/entities/route_map_outlet.dart';

abstract class SupervisorRouteMapState extends Equatable {
  const SupervisorRouteMapState();
  @override
  List<Object?> get props => [];
}

class SupervisorRouteMapInitial extends SupervisorRouteMapState {
  const SupervisorRouteMapInitial();
}

class SupervisorRouteMapLoadingReps extends SupervisorRouteMapState {
  const SupervisorRouteMapLoadingReps();
}

class SupervisorRouteMapRepsError extends SupervisorRouteMapState {
  final String message;
  const SupervisorRouteMapRepsError(this.message);
  @override
  List<Object?> get props => [message];
}

class SupervisorRouteMapReady extends SupervisorRouteMapState {
  final List<RepSummary> reps;
  final RepSummary? selectedRep;
  final bool isLoadingMap;
  final String? mapError;

  const SupervisorRouteMapReady({
    required this.reps,
    this.selectedRep,
    this.isLoadingMap = false,
    this.mapError,
  });

  bool get canLoad => selectedRep != null && !isLoadingMap;

  SupervisorRouteMapReady copyWith({
    List<RepSummary>? reps,
    RepSummary? selectedRep,
    bool? isLoadingMap,
    String? mapError,
    bool clearMapError = false,
  }) {
    return SupervisorRouteMapReady(
      reps: reps ?? this.reps,
      selectedRep: selectedRep ?? this.selectedRep,
      isLoadingMap: isLoadingMap ?? this.isLoadingMap,
      mapError: clearMapError ? null : (mapError ?? this.mapError),
    );
  }

  @override
  List<Object?> get props => [reps, selectedRep, isLoadingMap, mapError];
}

class SupervisorRouteMapLoaded extends SupervisorRouteMapState {
  final List<RouteMapOutlet> outlets;
  final RepSummary rep;

  const SupervisorRouteMapLoaded({
    required this.outlets,
    required this.rep,
  });

  @override
  List<Object?> get props => [outlets, rep];
}
