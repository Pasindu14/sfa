import 'package:equatable/equatable.dart';
import 'package:uswatte/features/purchase_orders/domain/entities/purchase_order_detail.dart';
import 'package:uswatte/features/purchase_orders/domain/entities/purchase_order_summary.dart';

sealed class PurchaseOrdersState extends Equatable {
  const PurchaseOrdersState();
  @override
  List<Object?> get props => [];
}

final class PurchaseOrdersInitial extends PurchaseOrdersState {
  const PurchaseOrdersInitial();
}

final class PurchaseOrdersLoading extends PurchaseOrdersState {
  const PurchaseOrdersLoading();
}

final class PurchaseOrdersLoaded extends PurchaseOrdersState {
  final List<PurchaseOrderSummary> orders;
  const PurchaseOrdersLoaded(this.orders);
  @override
  List<Object?> get props => [orders];
}

final class PurchaseOrderDetailLoading extends PurchaseOrdersState {
  const PurchaseOrderDetailLoading();
}

final class PurchaseOrderDetailLoaded extends PurchaseOrdersState {
  final PurchaseOrderDetail order;
  const PurchaseOrderDetailLoaded(this.order);
  @override
  List<Object?> get props => [order];
}

final class PurchaseOrderActionInProgress extends PurchaseOrdersState {
  const PurchaseOrderActionInProgress();
}

final class PurchaseOrderActionSuccess extends PurchaseOrdersState {
  final String message;
  const PurchaseOrderActionSuccess(this.message);
  @override
  List<Object?> get props => [message];
}

final class PurchaseOrdersError extends PurchaseOrdersState {
  final String message;
  const PurchaseOrdersError(this.message);
  @override
  List<Object?> get props => [message];
}
