part of 'rep_assignment_bloc.dart';

abstract class RepAssignmentState extends Equatable {
  const RepAssignmentState();

  /// True only when the server has said the rep has no distributor — no geo
  /// assignment at all, or a territory with no distributor. Loading and network
  /// failures are "unknown", not "none": the app bills offline, so an unreachable
  /// server must never lock the rep out of New Order.
  bool get hasNoDistributor => switch (this) {
        RepAssignmentLoaded(:final assignment) => assignment.distributorId == null,
        RepAssignmentError(:final notAssigned) => notAssigned,
        _ => false,
      };

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

  /// The server answered that the rep has no active assignment (404), as opposed
  /// to the request failing.
  final bool notAssigned;

  const RepAssignmentError(this.message, {this.notAssigned = false});

  @override
  List<Object?> get props => [message, notAssigned];
}
