import 'package:equatable/equatable.dart';
import 'package:uswatte/features/route_assignment/domain/entities/rep_summary.dart';

abstract class SupervisorRouteMapEvent extends Equatable {
  const SupervisorRouteMapEvent();
}

class SupervisorRouteMapRepsRequested extends SupervisorRouteMapEvent {
  const SupervisorRouteMapRepsRequested();
  @override
  List<Object?> get props => [];
}

class SupervisorRouteMapRepSelected extends SupervisorRouteMapEvent {
  final RepSummary rep;
  const SupervisorRouteMapRepSelected(this.rep);
  @override
  List<Object?> get props => [rep];
}

class SupervisorRouteMapLoadRequested extends SupervisorRouteMapEvent {
  const SupervisorRouteMapLoadRequested();
  @override
  List<Object?> get props => [];
}

class SupervisorRouteMapRefreshRequested extends SupervisorRouteMapEvent {
  const SupervisorRouteMapRefreshRequested();
  @override
  List<Object?> get props => [];
}

class SupervisorRouteMapBackToSelectorRequested extends SupervisorRouteMapEvent {
  const SupervisorRouteMapBackToSelectorRequested();
  @override
  List<Object?> get props => [];
}
