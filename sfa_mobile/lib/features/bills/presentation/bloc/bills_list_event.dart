import 'dart:async';

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

  /// Completed once the delete has been decided: null when the bill was
  /// deleted, otherwise a rep-friendly reason it was refused (e.g. no
  /// internet to check the server, or the bill is being sent right now).
  final Completer<String?>? result;

  const DeleteBillRequested(this.clientBillId, {this.result});
  @override
  List<Object?> get props => [clientBillId];
}

final class FlushAllRequested extends BillsListEvent {
  const FlushAllRequested();
}
