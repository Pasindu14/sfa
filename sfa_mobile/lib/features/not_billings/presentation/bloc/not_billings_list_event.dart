import 'dart:async';

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

  /// Completed once the delete has been decided: null when deleted, otherwise
  /// a rep-friendly reason it was refused (e.g. the visit is being sent now).
  final Completer<String?>? result;

  const DeleteNotBillingRequested(this.clientNotBillingId, {this.result});
  @override
  List<Object?> get props => [clientNotBillingId];
}

final class FlushAllNotBillingsRequested extends NotBillingsListEvent {
  const FlushAllNotBillingsRequested();
}
