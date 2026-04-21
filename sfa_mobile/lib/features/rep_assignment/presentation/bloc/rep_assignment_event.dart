part of 'rep_assignment_bloc.dart';

abstract class RepAssignmentEvent extends Equatable {
  const RepAssignmentEvent();

  @override
  List<Object?> get props => [];
}

class LoadRepAssignmentRequested extends RepAssignmentEvent {
  const LoadRepAssignmentRequested();
}
