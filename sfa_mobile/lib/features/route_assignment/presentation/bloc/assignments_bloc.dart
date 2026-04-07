import 'package:bloc/bloc.dart';
import 'package:equatable/equatable.dart';
import 'package:uswatte/core/errors/app_exception.dart';
import 'package:uswatte/features/route_assignment/domain/entities/daily_route_assignment.dart';
import 'package:uswatte/features/route_assignment/domain/usecases/delete_assignment_usecase.dart';
import 'package:uswatte/features/route_assignment/domain/usecases/get_assignments_usecase.dart';

part 'assignments_event.dart';
part 'assignments_state.dart';

class AssignmentsBloc extends Bloc<AssignmentsEvent, AssignmentsState> {
  final GetAssignmentsUseCase _getAssignments;
  final DeleteAssignmentUseCase _deleteAssignment;

  AssignmentsBloc({
    required GetAssignmentsUseCase getAssignments,
    required DeleteAssignmentUseCase deleteAssignment,
  })  : _getAssignments = getAssignments,
        _deleteAssignment = deleteAssignment,
        super(AssignmentsInitial()) {
    on<LoadAssignmentsRequested>(_onLoad);
    on<DateChanged>(_onDateChanged);
    on<DeleteAssignmentRequested>(_onDelete);
  }

  Future<void> _onLoad(
    LoadAssignmentsRequested event,
    Emitter<AssignmentsState> emit,
  ) async {
    emit(AssignmentsLoading(date: event.date));
    try {
      final result = await _getAssignments(date: event.date);
      emit(AssignmentsLoaded(
        assignments: result.assignments,
        totalCount: result.totalCount,
        date: event.date,
      ));
    } on AppException catch (e) {
      emit(AssignmentsError(message: e.message, date: event.date));
    } catch (_) {
      emit(AssignmentsError(
          message: 'Failed to load assignments.', date: event.date));
    }
  }

  Future<void> _onDateChanged(
    DateChanged event,
    Emitter<AssignmentsState> emit,
  ) async {
    emit(AssignmentsLoading(date: event.date));
    try {
      final result = await _getAssignments(date: event.date);
      emit(AssignmentsLoaded(
        assignments: result.assignments,
        totalCount: result.totalCount,
        date: event.date,
      ));
    } on AppException catch (e) {
      emit(AssignmentsError(message: e.message, date: event.date));
    } catch (_) {
      emit(AssignmentsError(
          message: 'Failed to load assignments.', date: event.date));
    }
  }

  Future<void> _onDelete(
    DeleteAssignmentRequested event,
    Emitter<AssignmentsState> emit,
  ) async {
    final current = state;
    if (current is! AssignmentsLoaded) return;

    emit(current.copyWith(deletingId: event.id));
    try {
      await _deleteAssignment(event.id);
      final updated =
          current.assignments.where((a) => a.id != event.id).toList();
      emit(AssignmentsLoaded(
        assignments: updated,
        totalCount: current.totalCount - 1,
        date: current.date,
      ));
    } on AppException catch (e) {
      emit(AssignmentsLoaded(
        assignments: current.assignments,
        totalCount: current.totalCount,
        date: current.date,
        deleteError: e.message,
      ));
    } catch (_) {
      emit(AssignmentsLoaded(
        assignments: current.assignments,
        totalCount: current.totalCount,
        date: current.date,
        deleteError: 'Failed to cancel assignment.',
      ));
    }
  }
}
