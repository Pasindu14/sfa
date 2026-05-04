import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:uswatte/features/route_assignment/domain/usecases/get_my_reps_usecase.dart';
import 'package:uswatte/features/supervisor_not_billing/domain/usecases/get_supervisor_not_billings_usecase.dart';
import 'package:uswatte/features/supervisor_not_billing/presentation/bloc/supervisor_not_billing_event.dart';
import 'package:uswatte/features/supervisor_not_billing/presentation/bloc/supervisor_not_billing_state.dart';

class SupervisorNotBillingBloc
    extends Bloc<SupervisorNotBillingEvent, SupervisorNotBillingState> {
  final GetMyRepsUseCase _getMyReps;
  final GetSupervisorNotBillingsUseCase _getSupervisorNotBillings;

  SupervisorNotBillingBloc({
    required GetMyRepsUseCase getMyReps,
    required GetSupervisorNotBillingsUseCase getSupervisorNotBillings,
  })  : _getMyReps = getMyReps,
        _getSupervisorNotBillings = getSupervisorNotBillings,
        super(const SupervisorNotBillingInitial()) {
    on<LoadRepsRequested>(_onLoadReps);
    on<RepSelected>(_onRepSelected);
    on<DateSelected>(_onDateSelected);
    on<LoadNotBillingsRequested>(_onLoadNotBillings);
  }

  Future<void> _onLoadReps(
      LoadRepsRequested event, Emitter<SupervisorNotBillingState> emit) async {
    emit(const SupervisorNotBillingLoading());
    try {
      final reps = await _getMyReps();
      emit(SupervisorNotBillingReady(
        reps: reps,
        selectedDate: DateTime.now(),
      ));
    } catch (e) {
      emit(const SupervisorNotBillingError('Failed to load sales reps.'));
    }
  }

  void _onRepSelected(
      RepSelected event, Emitter<SupervisorNotBillingState> emit) {
    final current = state;
    if (current is! SupervisorNotBillingReady) return;
    emit(current.copyWith(
      selectedRep: event.rep,
      clearNotBillings: true,
      clearNotBillingsError: true,
    ));
  }

  void _onDateSelected(
      DateSelected event, Emitter<SupervisorNotBillingState> emit) {
    final current = state;
    if (current is! SupervisorNotBillingReady) return;
    emit(current.copyWith(
      selectedDate: event.date,
      clearNotBillings: true,
      clearNotBillingsError: true,
    ));
  }

  Future<void> _onLoadNotBillings(
      LoadNotBillingsRequested event,
      Emitter<SupervisorNotBillingState> emit) async {
    final current = state;
    if (current is! SupervisorNotBillingReady) return;
    if (current.selectedRep == null) return;

    emit(current.copyWith(
      isLoadingNotBillings: true,
      clearNotBillings: true,
      clearNotBillingsError: true,
    ));

    try {
      final d = current.selectedDate;
      final date =
          '${d.year}-${d.month.toString().padLeft(2, '0')}-${d.day.toString().padLeft(2, '0')}';

      final notBillings = await _getSupervisorNotBillings(
        salesRepId: current.selectedRep!.userId,
        date: date,
      );

      emit(current.copyWith(
        notBillings: notBillings,
        isLoadingNotBillings: false,
      ));
    } catch (e) {
      emit(current.copyWith(
        isLoadingNotBillings: false,
        notBillingsError: 'Failed to load non-billings.',
      ));
    }
  }
}
