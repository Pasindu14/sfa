import 'dart:async';

import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:uswatte/core/errors/app_exception.dart';
import 'package:uswatte/core/sync/not_billing_sync_service.dart';
import 'package:uswatte/features/not_billings/domain/usecases/delete_not_billing_usecase.dart';
import 'package:uswatte/features/not_billings/domain/usecases/get_not_billings_usecase.dart';
import 'package:uswatte/features/not_billings/domain/usecases/retry_not_billing_sync_usecase.dart';
import 'package:uswatte/features/not_billings/presentation/bloc/not_billings_list_event.dart';
import 'package:uswatte/features/not_billings/presentation/bloc/not_billings_list_state.dart';

class NotBillingsListBloc
    extends Bloc<NotBillingsListEvent, NotBillingsListState> {
  final GetNotBillingsUseCase _getNotBillings;
  final RetryNotBillingSyncUseCase _retrySync;
  final DeleteNotBillingUseCase _deleteNotBilling;
  final NotBillingSyncService _syncService;

  StreamSubscription<NotBillingOutboxStatus>? _statusSub;

  NotBillingsListBloc({
    required GetNotBillingsUseCase getNotBillingsUseCase,
    required RetryNotBillingSyncUseCase retrySyncUseCase,
    required DeleteNotBillingUseCase deleteNotBillingUseCase,
    required NotBillingSyncService syncService,
  })  : _getNotBillings = getNotBillingsUseCase,
        _retrySync = retrySyncUseCase,
        _deleteNotBilling = deleteNotBillingUseCase,
        _syncService = syncService,
        super(const NotBillingsListInitial()) {
    on<LoadNotBillingsRequested>(_onLoad);
    on<NotBillingsOutboxChanged>(_onOutboxChanged);
    on<RetryNotBillingRequested>(_onRetry);
    on<DeleteNotBillingRequested>(_onDelete);
    on<FlushAllNotBillingsRequested>(_onFlushAll);

    _statusSub = _syncService.status$.listen((_) {
      add(const NotBillingsOutboxChanged());
    });
  }

  Future<void> _onLoad(
      LoadNotBillingsRequested e, Emitter<NotBillingsListState> emit) async {
    emit(const NotBillingsListLoading());
    try {
      final records = await _getNotBillings(limit: 200);
      final pending = records
          .where((r) =>
              r.syncStatus.name == 'pending' || r.syncStatus.name == 'failed')
          .length;
      emit(NotBillingsListLoaded(records: records, pendingOrFailedCount: pending));
      // Kick a sync pass every time the list loads — catches records that were
      // created while no subscriber was on status$ (e.g. during navigation).
      _syncService.flushAll();
    } on AppException catch (ex) {
      emit(NotBillingsListError(ex.message));
    }
  }

  Future<void> _onOutboxChanged(
      NotBillingsOutboxChanged e, Emitter<NotBillingsListState> emit) async {
    final records = await _getNotBillings(limit: 200);
    final pending = records
        .where((r) =>
            r.syncStatus.name == 'pending' || r.syncStatus.name == 'failed')
        .length;
    emit(NotBillingsListLoaded(records: records, pendingOrFailedCount: pending));
  }

  Future<void> _onRetry(
      RetryNotBillingRequested e, Emitter<NotBillingsListState> emit) async {
    await _retrySync(e.clientNotBillingId);
  }

  Future<void> _onDelete(
      DeleteNotBillingRequested e, Emitter<NotBillingsListState> emit) async {
    await _deleteNotBilling(e.clientNotBillingId);
    add(const LoadNotBillingsRequested());
  }

  Future<void> _onFlushAll(
      FlushAllNotBillingsRequested e, Emitter<NotBillingsListState> emit) async {
    await _syncService.flushAll();
  }

  @override
  Future<void> close() async {
    await _statusSub?.cancel();
    return super.close();
  }
}
