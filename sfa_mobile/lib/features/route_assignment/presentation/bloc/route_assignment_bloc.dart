import 'package:bloc/bloc.dart';
import 'package:equatable/equatable.dart';
import 'package:uswatte/core/errors/app_exception.dart';
import 'package:uswatte/features/route_assignment/domain/entities/rep_route.dart';
import 'package:uswatte/features/route_assignment/domain/entities/rep_summary.dart';
import 'package:uswatte/features/route_assignment/domain/usecases/create_assignment_usecase.dart';
import 'package:uswatte/features/route_assignment/domain/usecases/get_my_reps_usecase.dart';
import 'package:uswatte/features/route_assignment/domain/usecases/get_rep_routes_usecase.dart';

part 'route_assignment_event.dart';
part 'route_assignment_state.dart';

class RouteAssignmentBloc
    extends Bloc<RouteAssignmentEvent, RouteAssignmentState> {
  final GetMyRepsUseCase _getMyReps;
  final GetRepRoutesUseCase _getRepRoutes;
  final CreateAssignmentUseCase _createAssignment;

  RouteAssignmentBloc({
    required GetMyRepsUseCase getMyReps,
    required GetRepRoutesUseCase getRepRoutes,
    required CreateAssignmentUseCase createAssignment,
  })  : _getMyReps = getMyReps,
        _getRepRoutes = getRepRoutes,
        _createAssignment = createAssignment,
        super(const RouteAssignmentInitial()) {
    on<LoadRepsRequested>(_onLoadReps);
    on<RepSelected>(_onRepSelected);
    on<RouteSelected>(_onRouteSelected);
    on<DateSelected>(_onDateSelected);
    on<AssignmentSubmitted>(_onSubmit);
  }

  Future<void> _onLoadReps(
    LoadRepsRequested event,
    Emitter<RouteAssignmentState> emit,
  ) async {
    emit(const RouteAssignmentLoadingReps());
    try {
      final reps = await _getMyReps();
      // Emit ready state with empty rep selection so the UI can show the list
      if (reps.isEmpty) {
        emit(const RouteAssignmentLoadError(
            'No sales reps report to you. Assign reporting lines first.'));
        return;
      }
      // Pre-select first rep and load their routes automatically
      final firstRep = reps.first;
      emit(RouteAssignmentLoadingRoutes(reps: reps, selectedRep: firstRep));
      await _loadRoutes(reps, firstRep, emit);
    } on AppException catch (e) {
      emit(RouteAssignmentLoadError(e.message));
    } catch (_) {
      emit(const RouteAssignmentLoadError('Failed to load reps.'));
    }
  }

  Future<void> _onRepSelected(
    RepSelected event,
    Emitter<RouteAssignmentState> emit,
  ) async {
    final currentState = state;
    List<RepSummary> reps;

    if (currentState is RouteAssignmentReady) {
      reps = currentState.reps;
    } else {
      return;
    }

    emit(RouteAssignmentLoadingRoutes(reps: reps, selectedRep: event.rep));
    await _loadRoutes(reps, event.rep, emit);
  }

  Future<void> _loadRoutes(
    List<RepSummary> reps,
    RepSummary rep,
    Emitter<RouteAssignmentState> emit,
  ) async {
    try {
      final routes = await _getRepRoutes(rep.userId);
      emit(RouteAssignmentReady(
        reps: reps,
        selectedRep: rep,
        routes: routes,
        selectedRoute: routes.isNotEmpty ? routes.first : null,
      ));
    } on AppException catch (e) {
      emit(RouteAssignmentLoadError(e.message));
    } catch (_) {
      emit(const RouteAssignmentLoadError('Failed to load routes.'));
    }
  }

  void _onRouteSelected(
    RouteSelected event,
    Emitter<RouteAssignmentState> emit,
  ) {
    final current = state;
    if (current is RouteAssignmentReady) {
      emit(current.copyWith(selectedRoute: event.route));
    }
  }

  void _onDateSelected(
    DateSelected event,
    Emitter<RouteAssignmentState> emit,
  ) {
    final current = state;
    if (current is RouteAssignmentReady) {
      emit(current.copyWith(selectedDate: event.date));
    }
  }

  Future<void> _onSubmit(
    AssignmentSubmitted event,
    Emitter<RouteAssignmentState> emit,
  ) async {
    final current = state;
    if (current is! RouteAssignmentReady || !current.canSubmit) return;

    emit(RouteAssignmentSaving(current));
    try {
      await _createAssignment(
        userId: current.selectedRep.userId,
        routeId: current.selectedRoute!.routeId,
        assignedDate: current.selectedDate!,
      );
      emit(const RouteAssignmentSuccess());
    } on AppException catch (e) {
      emit(RouteAssignmentError(message: e.message, formState: current));
    } catch (_) {
      emit(RouteAssignmentError(
          message: 'An unexpected error occurred. Please try again.',
          formState: current));
    }
  }
}
