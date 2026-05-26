import 'package:bloc_concurrency/bloc_concurrency.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:uswatte/core/errors/app_exception.dart';
import 'package:uswatte/features/purchase_orders/domain/usecases/get_pending_purchase_orders_usecase.dart';
import 'package:uswatte/features/purchase_orders/domain/usecases/get_purchase_order_usecase.dart';
import 'package:uswatte/features/purchase_orders/domain/usecases/manager_approve_purchase_order_usecase.dart';
import 'package:uswatte/features/purchase_orders/domain/usecases/rep_approve_purchase_order_usecase.dart';
import 'package:uswatte/features/purchase_orders/domain/usecases/reject_purchase_order_usecase.dart';
import 'purchase_orders_event.dart';
import 'purchase_orders_state.dart';

class PurchaseOrdersBloc extends Bloc<PurchaseOrdersEvent, PurchaseOrdersState> {
  final GetPendingPurchaseOrdersUseCase _getPendingOrders;
  final GetPurchaseOrderUseCase _getOrderDetail;
  final RepApprovePurchaseOrderUseCase _repApprove;
  final ManagerApprovePurchaseOrderUseCase _managerApprove;
  final RejectPurchaseOrderUseCase _rejectOrder;
  final String _statusFilter;

  PurchaseOrdersBloc({
    required GetPendingPurchaseOrdersUseCase getPendingOrders,
    required GetPurchaseOrderUseCase getOrderDetail,
    required RepApprovePurchaseOrderUseCase repApprove,
    required ManagerApprovePurchaseOrderUseCase managerApprove,
    required RejectPurchaseOrderUseCase rejectOrder,
    String statusFilter = 'PendingRepApproval',
  })  : _getPendingOrders = getPendingOrders,
        _getOrderDetail = getOrderDetail,
        _repApprove = repApprove,
        _managerApprove = managerApprove,
        _rejectOrder = rejectOrder,
        _statusFilter = statusFilter,
        super(const PurchaseOrdersInitial()) {
    on<LoadPendingOrders>(_onLoad);
    on<RefreshOrders>(_onRefresh);
    on<LoadOrderDetail>(_onLoadDetail);
    on<RepApproveOrder>(_onRepApprove, transformer: sequential());
    on<ManagerApproveOrder>(_onManagerApprove, transformer: sequential());
    on<RejectOrder>(_onReject, transformer: sequential());
  }

  Future<void> _onLoad(
      LoadPendingOrders event, Emitter<PurchaseOrdersState> emit) async {
    emit(const PurchaseOrdersLoading());
    try {
      final orders = await _getPendingOrders(status: _statusFilter);
      emit(PurchaseOrdersLoaded(orders));
    } on AppException catch (e) {
      emit(PurchaseOrdersError(e.message));
    }
  }

  Future<void> _onRefresh(
      RefreshOrders event, Emitter<PurchaseOrdersState> emit) async {
    try {
      final orders = await _getPendingOrders(status: _statusFilter);
      emit(PurchaseOrdersLoaded(orders));
    } on AppException catch (e) {
      emit(PurchaseOrdersError(e.message));
    }
  }

  Future<void> _onLoadDetail(
      LoadOrderDetail event, Emitter<PurchaseOrdersState> emit) async {
    emit(const PurchaseOrderDetailLoading());
    try {
      final order = await _getOrderDetail(event.id);
      emit(PurchaseOrderDetailLoaded(order));
    } on AppException catch (e) {
      emit(PurchaseOrdersError(e.message));
    }
  }

  Future<void> _onRepApprove(
      RepApproveOrder event, Emitter<PurchaseOrdersState> emit) async {
    final s = state;
    if (s is! PurchaseOrderDetailLoaded) return;
    emit(PurchaseOrderActionInProgress(s.order));
    try {
      await _repApprove(event.id);
      emit(const PurchaseOrderActionSuccess('Purchase order approved.'));
    } on AppException catch (e) {
      emit(PurchaseOrderDetailLoaded(s.order));
      emit(PurchaseOrdersError(e.message));
    }
  }

  Future<void> _onManagerApprove(
      ManagerApproveOrder event, Emitter<PurchaseOrdersState> emit) async {
    final s = state;
    if (s is! PurchaseOrderDetailLoaded) return;
    emit(PurchaseOrderActionInProgress(s.order));
    try {
      await _managerApprove(event.id);
      emit(const PurchaseOrderActionSuccess('Purchase order approved.'));
    } on AppException catch (e) {
      emit(PurchaseOrderDetailLoaded(s.order));
      emit(PurchaseOrdersError(e.message));
    }
  }

  Future<void> _onReject(
      RejectOrder event, Emitter<PurchaseOrdersState> emit) async {
    final s = state;
    if (s is! PurchaseOrderDetailLoaded) return;
    emit(PurchaseOrderActionInProgress(s.order));
    try {
      await _rejectOrder(event.id, event.reason);
      emit(const PurchaseOrderActionSuccess('Purchase order rejected.'));
    } on AppException catch (e) {
      emit(PurchaseOrderDetailLoaded(s.order));
      emit(PurchaseOrdersError(e.message));
    }
  }
}
