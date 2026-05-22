import 'package:equatable/equatable.dart';

sealed class PurchaseOrdersEvent extends Equatable {
  const PurchaseOrdersEvent();
  @override
  List<Object?> get props => [];
}

final class LoadPendingOrders extends PurchaseOrdersEvent {
  const LoadPendingOrders();
}

final class RefreshOrders extends PurchaseOrdersEvent {
  const RefreshOrders();
}

final class LoadOrderDetail extends PurchaseOrdersEvent {
  final int id;
  const LoadOrderDetail(this.id);
  @override
  List<Object?> get props => [id];
}

final class RepApproveOrder extends PurchaseOrdersEvent {
  final int id;
  const RepApproveOrder(this.id);
  @override
  List<Object?> get props => [id];
}

final class ManagerApproveOrder extends PurchaseOrdersEvent {
  final int id;
  const ManagerApproveOrder(this.id);
  @override
  List<Object?> get props => [id];
}

final class RejectOrder extends PurchaseOrdersEvent {
  final int id;
  final String reason;
  const RejectOrder(this.id, this.reason);
  @override
  List<Object?> get props => [id, reason];
}
