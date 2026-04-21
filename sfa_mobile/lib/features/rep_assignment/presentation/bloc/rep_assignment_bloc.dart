import 'package:bloc/bloc.dart';
import 'package:equatable/equatable.dart';
import 'package:uswatte/core/errors/app_exception.dart';
import 'package:uswatte/features/rep_assignment/domain/entities/rep_assignment.dart';
import 'package:uswatte/features/rep_assignment/domain/usecases/get_rep_assignment_usecase.dart';

part 'rep_assignment_event.dart';
part 'rep_assignment_state.dart';

class RepAssignmentBloc
    extends Bloc<RepAssignmentEvent, RepAssignmentState> {
  final GetRepAssignmentUseCase _getRepAssignment;

  RepAssignmentBloc({required GetRepAssignmentUseCase getRepAssignment})
      : _getRepAssignment = getRepAssignment,
        super(const RepAssignmentInitial()) {
    on<LoadRepAssignmentRequested>(_onLoad);
  }

  Future<void> _onLoad(
    LoadRepAssignmentRequested event,
    Emitter<RepAssignmentState> emit,
  ) async {
    emit(const RepAssignmentLoading());
    try {
      final assignment = await _getRepAssignment();
      emit(RepAssignmentLoaded(assignment));
    } on AppException catch (e) {
      emit(RepAssignmentError(e.message));
    } catch (_) {
      emit(const RepAssignmentError('Failed to load assignment.'));
    }
  }
}
