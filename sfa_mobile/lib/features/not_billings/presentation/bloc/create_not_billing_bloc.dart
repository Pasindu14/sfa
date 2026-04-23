import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:uswatte/core/errors/app_exception.dart';
import 'package:uswatte/features/bills/domain/entities/sync_status.dart';
import 'package:uswatte/features/not_billings/domain/entities/not_billing.dart';
import 'package:uswatte/features/not_billings/domain/usecases/create_not_billing_usecase.dart';
import 'package:uswatte/features/not_billings/presentation/bloc/create_not_billing_event.dart';
import 'package:uswatte/features/not_billings/presentation/bloc/create_not_billing_state.dart';
import 'package:uuid/uuid.dart';

class CreateNotBillingBloc
    extends Bloc<CreateNotBillingEvent, CreateNotBillingState> {
  final CreateNotBillingUseCase _createNotBilling;
  final Uuid _uuid;

  CreateNotBillingBloc({
    required CreateNotBillingUseCase createNotBillingUseCase,
    Uuid? uuid,
  })  : _createNotBilling = createNotBillingUseCase,
        _uuid = uuid ?? const Uuid(),
        super(const CreateNotBillingState()) {
    on<OutletSelectedForNotBilling>(_onOutletSelected);
    on<NotBillingReasonSelected>(_onReasonSelected);
    on<NotBillingNotesChanged>(_onNotesChanged);
    on<SubmitNotBillingPressed>(_onSubmit);
  }

  void _onOutletSelected(
      OutletSelectedForNotBilling e, Emitter<CreateNotBillingState> emit) {
    emit(state.copyWith(
      outletId: e.outletId,
      outletName: e.outletName,
      routeName: e.routeName,
      clearError: true,
    ));
  }

  void _onReasonSelected(
      NotBillingReasonSelected e, Emitter<CreateNotBillingState> emit) {
    emit(state.copyWith(reason: e.reason, clearError: true));
  }

  void _onNotesChanged(
      NotBillingNotesChanged e, Emitter<CreateNotBillingState> emit) {
    emit(state.copyWith(notes: e.notes));
  }

  Future<void> _onSubmit(
      SubmitNotBillingPressed e, Emitter<CreateNotBillingState> emit) async {
    if (!state.canSubmit) return;

    emit(state.copyWith(submitting: true, clearError: true));

    try {
      final clientId = _uuid.v4();
      final now = DateTime.now();

      final record = NotBilling(
        clientNotBillingId: clientId,
        outletId: state.outletId!,
        outletName: state.outletName,
        routeName: state.routeName,
        notBillingDate: DateTime(now.year, now.month, now.day),
        reason: state.reason!,
        notes: state.notes?.trim().isEmpty == true ? null : state.notes?.trim(),
        createdAt: now,
        syncStatus: SyncStatus.pending,
      );

      await _createNotBilling(record);
      emit(state.copyWith(submitting: false, submittedClientId: clientId));
    } on AppException catch (ex) {
      emit(state.copyWith(submitting: false, errorMessage: ex.message));
    } catch (_) {
      emit(state.copyWith(
        submitting: false,
        errorMessage: 'An unexpected error occurred.',
      ));
    }
  }
}
