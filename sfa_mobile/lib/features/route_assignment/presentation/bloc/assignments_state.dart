part of 'assignments_bloc.dart';

abstract class AssignmentsState extends Equatable {
  const AssignmentsState();

  @override
  List<Object?> get props => [];
}

class AssignmentsInitial extends AssignmentsState {}

class AssignmentsLoading extends AssignmentsState {
  final DateTime? date;
  const AssignmentsLoading({this.date});

  @override
  List<Object?> get props => [date];
}

class AssignmentsLoaded extends AssignmentsState {
  final List<DailyRouteAssignment> assignments;
  final int totalCount;
  final DateTime? date;
  final int? deletingId;
  final String? deleteError;

  const AssignmentsLoaded({
    required this.assignments,
    required this.totalCount,
    required this.date,
    this.deletingId,
    this.deleteError,
  });

  AssignmentsLoaded copyWith({
    List<DailyRouteAssignment>? assignments,
    int? totalCount,
    DateTime? date,
    int? deletingId,
    String? deleteError,
  }) =>
      AssignmentsLoaded(
        assignments: assignments ?? this.assignments,
        totalCount: totalCount ?? this.totalCount,
        date: date ?? this.date,
        deletingId: deletingId,
        deleteError: deleteError,
      );

  @override
  List<Object?> get props =>
      [assignments, totalCount, date, deletingId, deleteError];
}

class AssignmentsError extends AssignmentsState {
  final String message;
  final DateTime? date;

  const AssignmentsError({required this.message, this.date});

  @override
  List<Object?> get props => [message, date];
}
