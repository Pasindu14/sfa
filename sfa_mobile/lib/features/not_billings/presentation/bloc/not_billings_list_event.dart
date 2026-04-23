import 'package:equatable/equatable.dart';

sealed class NotBillingsListEvent extends Equatable {
  const NotBillingsListEvent();
  @override
  List<Object?> get props => [];
}

final class LoadNotBillingsRequested extends NotBillingsListEvent {
  const LoadNotBillingsRequested();
}

final class NotBillingsOutboxChanged extends NotBillingsListEvent {
  const NotBillingsOutboxChanged();
}

final class RetryNotBillingRequested extends NotBillingsListEvent {
  final String clientNotBillingId;
  const RetryNotBillingRequested(this.clientNotBillingId);
  @override
  List<Object?> get props => [clientNotBillingId];
}

final class DeleteNotBillingRequested extends NotBillingsListEvent {
  final String clientNotBillingId;
  const DeleteNotBillingRequested(this.clientNotBillingId);
  @override
  List<Object?> get props => [clientNotBillingId];
}

final class FlushAllNotBillingsRequested extends NotBillingsListEvent {
  const FlushAllNotBillingsRequested();
}
