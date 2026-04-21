part of 'rep_assignment_bloc.dart';

abstract class RepAssignmentState extends Equatable {
  const RepAssignmentState();

  @override
  List<Object?> get props => [];
}

class RepAssignmentInitial extends RepAssignmentState {
  const RepAssignmentInitial();
}

class RepAssignmentLoading extends RepAssignmentState {
  const RepAssignmentLoading();
}

class RepAssignmentLoaded extends RepAssignmentState {
  final RepAssignment assignment;
  const RepAssignmentLoaded(this.assignment);

  @override
  List<Object?> get props => [assignment];
}

class RepAssignmentError extends RepAssignmentState {
  final String message;
  const RepAssignmentError(this.message);

  @override
  List<Object?> get props => [message];
}
