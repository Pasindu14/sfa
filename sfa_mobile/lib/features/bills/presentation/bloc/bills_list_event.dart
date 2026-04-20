import 'package:equatable/equatable.dart';

sealed class BillsListEvent extends Equatable {
  const BillsListEvent();
  @override
  List<Object?> get props => [];
}

final class LoadBillsRequested extends BillsListEvent {
  const LoadBillsRequested();
}

final class BillsOutboxChanged extends BillsListEvent {
  const BillsOutboxChanged();
}

final class RetryBillRequested extends BillsListEvent {
  final String clientBillId;
  const RetryBillRequested(this.clientBillId);
  @override
  List<Object?> get props => [clientBillId];
}

final class DeleteBillRequested extends BillsListEvent {
  final String clientBillId;
  const DeleteBillRequested(this.clientBillId);
  @override
  List<Object?> get props => [clientBillId];
}

final class FlushAllRequested extends BillsListEvent {
  const FlushAllRequested();
}
