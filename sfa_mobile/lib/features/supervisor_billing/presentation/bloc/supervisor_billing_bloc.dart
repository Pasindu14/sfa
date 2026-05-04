import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:uswatte/features/route_assignment/domain/usecases/get_my_reps_usecase.dart';
import 'package:uswatte/features/supervisor_billing/domain/usecases/get_supervisor_billings_usecase.dart';
import 'package:uswatte/features/supervisor_billing/presentation/bloc/supervisor_billing_event.dart';
import 'package:uswatte/features/supervisor_billing/presentation/bloc/supervisor_billing_state.dart';

class SupervisorBillingBloc
    extends Bloc<SupervisorBillingEvent, SupervisorBillingState> {
  final GetMyRepsUseCase _getMyReps;
  final GetSupervisorBillingsUseCase _getSupervisorBillings;

  SupervisorBillingBloc({
    required GetMyRepsUseCase getMyReps,
    required GetSupervisorBillingsUseCase getSupervisorBillings,
  })  : _getMyReps = getMyReps,
        _getSupervisorBillings = getSupervisorBillings,
        super(const SupervisorBillingInitial()) {
    on<LoadRepsRequested>(_onLoadReps);
    on<RepSelected>(_onRepSelected);
    on<DateSelected>(_onDateSelected);
    on<LoadBillingsRequested>(_onLoadBillings);
  }

  Future<void> _onLoadReps(
    LoadRepsRequested event,
    Emitter<SupervisorBillingState> emit,
  ) async {
    emit(const SupervisorBillingLoadingReps());
    try {
      final reps = await _getMyReps();
      emit(SupervisorBillingReady(
        reps: reps,
        selectedDate: DateTime.now(),
      ));
    } catch (e) {
      emit(SupervisorBillingLoadError(
          'Failed to load reps. Please try again.'));
    }
  }

  void _onRepSelected(
    RepSelected event,
    Emitter<SupervisorBillingState> emit,
  ) {
    final current = state;
    if (current is! SupervisorBillingReady) return;
    emit(current.copyWith(
      selectedRep: event.rep,
      clearBillings: true,
      clearBillingsError: true,
    ));
  }

  void _onDateSelected(
    DateSelected event,
    Emitter<SupervisorBillingState> emit,
  ) {
    final current = state;
    if (current is! SupervisorBillingReady) return;
    emit(current.copyWith(
      selectedDate: event.date,
      clearBillings: true,
      clearBillingsError: true,
    ));
  }

  Future<void> _onLoadBillings(
    LoadBillingsRequested event,
    Emitter<SupervisorBillingState> emit,
  ) async {
    final current = state;
    if (current is! SupervisorBillingReady) return;
    if (current.selectedRep == null) return;

    emit(current.copyWith(
      isLoadingBillings: true,
      clearBillings: true,
      clearBillingsError: true,
    ));

    try {
      final date = current.selectedDate;
      final dateStr =
          '${date.year}-${date.month.toString().padLeft(2, '0')}-${date.day.toString().padLeft(2, '0')}';

      final billings = await _getSupervisorBillings(
        salesRepId: current.selectedRep!.userId,
        date: dateStr,
      );

      final ready = state;
      if (ready is! SupervisorBillingReady) return;
      emit(ready.copyWith(
        billings: billings,
        isLoadingBillings: false,
      ));
    } catch (e) {
      final ready = state;
      if (ready is! SupervisorBillingReady) return;
      emit(ready.copyWith(
        isLoadingBillings: false,
        billingsError: 'Failed to load bills. Please try again.',
      ));
    }
  }
}
