part of 'route_assignment_bloc.dart';

abstract class RouteAssignmentState extends Equatable {
  const RouteAssignmentState();

  @override
  List<Object?> get props => [];
}

class RouteAssignmentInitial extends RouteAssignmentState {
  const RouteAssignmentInitial();
}

class RouteAssignmentLoadingReps extends RouteAssignmentState {
  const RouteAssignmentLoadingReps();
}

class RouteAssignmentLoadingRoutes extends RouteAssignmentState {
  final List<RepSummary> reps;
  final RepSummary selectedRep;

  const RouteAssignmentLoadingRoutes({
    required this.reps,
    required this.selectedRep,
  });

  @override
  List<Object> get props => [reps, selectedRep];
}

class RouteAssignmentReady extends RouteAssignmentState {
  final List<RepSummary> reps;
  final RepSummary selectedRep;
  final List<RepRoute> routes;
  final RepRoute? selectedRoute;
  final DateTime? selectedDate;

  const RouteAssignmentReady({
    required this.reps,
    required this.selectedRep,
    required this.routes,
    this.selectedRoute,
    this.selectedDate,
  });

  bool get canSubmit =>
      selectedRoute != null && selectedDate != null;

  RouteAssignmentReady copyWith({
    RepRoute? selectedRoute,
    DateTime? selectedDate,
    bool clearRoute = false,
    bool clearDate = false,
  }) =>
      RouteAssignmentReady(
        reps: reps,
        selectedRep: selectedRep,
        routes: routes,
        selectedRoute: clearRoute ? null : selectedRoute ?? this.selectedRoute,
        selectedDate: clearDate ? null : selectedDate ?? this.selectedDate,
      );

  @override
  List<Object?> get props =>
      [reps, selectedRep, routes, selectedRoute, selectedDate];
}

class RouteAssignmentSaving extends RouteAssignmentState {
  final RouteAssignmentReady formState;
  const RouteAssignmentSaving(this.formState);

  @override
  List<Object> get props => [formState];
}

class RouteAssignmentSuccess extends RouteAssignmentState {
  const RouteAssignmentSuccess();
}

class RouteAssignmentError extends RouteAssignmentState {
  final String message;
  final RouteAssignmentReady? formState;

  const RouteAssignmentError({required this.message, this.formState});

  @override
  List<Object?> get props => [message, formState];
}

class RouteAssignmentLoadError extends RouteAssignmentState {
  final String message;
  const RouteAssignmentLoadError(this.message);

  @override
  List<Object> get props => [message];
}
