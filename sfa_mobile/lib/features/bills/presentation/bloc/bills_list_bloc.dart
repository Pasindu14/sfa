import 'dart:async';

import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:uswatte/core/errors/app_exception.dart';
import 'package:uswatte/core/sync/bill_sync_service.dart';
import 'package:uswatte/features/bills/domain/usecases/delete_bill_usecase.dart';
import 'package:uswatte/features/bills/domain/usecases/get_bills_usecase.dart';
import 'package:uswatte/features/bills/domain/usecases/retry_sync_usecase.dart';
import 'package:uswatte/features/bills/presentation/bloc/bills_list_event.dart';
import 'package:uswatte/features/bills/presentation/bloc/bills_list_state.dart';

/// Loads local bills and stays live by subscribing to BillSyncService so chips
/// flip from yellow → green → red automatically as the outbox processes.
class BillsListBloc extends Bloc<BillsListEvent, BillsListState> {
  final GetBillsUseCase _getBills;
  final RetrySyncUseCase _retrySync;
  final DeleteBillUseCase _deleteBill;
  final BillSyncService _syncService;

  StreamSubscription<BillOutboxStatus>? _statusSub;

  BillsListBloc({
    required GetBillsUseCase getBillsUseCase,
    required RetrySyncUseCase retrySyncUseCase,
    required DeleteBillUseCase deleteBillUseCase,
    required BillSyncService syncService,
  })  : _getBills = getBillsUseCase,
        _retrySync = retrySyncUseCase,
        _deleteBill = deleteBillUseCase,
        _syncService = syncService,
        super(const BillsListInitial()) {
    on<LoadBillsRequested>(_onLoad);
    on<BillsOutboxChanged>(_onOutboxChanged);
    on<RetryBillRequested>(_onRetry);
    on<DeleteBillRequested>(_onDelete);
    on<FlushAllRequested>(_onFlushAll);

    _statusSub = _syncService.status$.listen((_) {
      add(const BillsOutboxChanged());
    });
  }

  Future<void> _onLoad(
      LoadBillsRequested e, Emitter<BillsListState> emit) async {
    emit(const BillsListLoading());
    try {
      final bills = await _getBills(limit: 200);
      final pending = bills
          .where((b) =>
              b.syncStatus.name == 'pending' || b.syncStatus.name == 'failed')
          .length;
      emit(BillsListLoaded(bills: bills, pendingOrFailedCount: pending));
    } on AppException catch (ex) {
      emit(BillsListError(ex.message));
    }
  }

  Future<void> _onOutboxChanged(
      BillsOutboxChanged e, Emitter<BillsListState> emit) async {
    final bills = await _getBills(limit: 200);
    final pending = bills
        .where((b) =>
            b.syncStatus.name == 'pending' || b.syncStatus.name == 'failed')
        .length;
    emit(BillsListLoaded(bills: bills, pendingOrFailedCount: pending));
  }

  Future<void> _onRetry(
      RetryBillRequested e, Emitter<BillsListState> emit) async {
    await _retrySync(e.clientBillId);
  }

  Future<void> _onDelete(
      DeleteBillRequested e, Emitter<BillsListState> emit) async {
    await _deleteBill(e.clientBillId);
    add(const LoadBillsRequested());
  }

  Future<void> _onFlushAll(
      FlushAllRequested e, Emitter<BillsListState> emit) async {
    await _syncService.flushAll();
  }

  @override
  Future<void> close() async {
    await _statusSub?.cancel();
    return super.close();
  }
}
