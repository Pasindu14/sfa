part of 'assignments_bloc.dart';

abstract class AssignmentsEvent extends Equatable {
  const AssignmentsEvent();

  @override
  List<Object?> get props => [];
}

class LoadAssignmentsRequested extends AssignmentsEvent {
  final DateTime? date;
  const LoadAssignmentsRequested({this.date});

  @override
  List<Object?> get props => [date];
}

class DateChanged extends AssignmentsEvent {
  final DateTime? date;
  const DateChanged(this.date);

  @override
  List<Object?> get props => [date];
}

class DeleteAssignmentRequested extends AssignmentsEvent {
  final int id;
  const DeleteAssignmentRequested(this.id);

  @override
  List<Object> get props => [id];
}
