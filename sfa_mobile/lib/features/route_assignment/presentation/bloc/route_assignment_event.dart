part of 'route_assignment_bloc.dart';

abstract class RouteAssignmentEvent extends Equatable {
  const RouteAssignmentEvent();

  @override
  List<Object?> get props => [];
}

class LoadRepsRequested extends RouteAssignmentEvent {
  const LoadRepsRequested();
}

class RepSelected extends RouteAssignmentEvent {
  final RepSummary rep;
  const RepSelected(this.rep);

  @override
  List<Object> get props => [rep];
}

class RouteSelected extends RouteAssignmentEvent {
  final RepRoute route;
  const RouteSelected(this.route);

  @override
  List<Object> get props => [route];
}

class DateSelected extends RouteAssignmentEvent {
  final DateTime date;
  const DateSelected(this.date);

  @override
  List<Object> get props => [date];
}

class AssignmentSubmitted extends RouteAssignmentEvent {
  const AssignmentSubmitted();
}
