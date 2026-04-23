import 'package:equatable/equatable.dart';
import 'package:uswatte/features/not_billings/domain/entities/not_billing.dart';

sealed class NotBillingsListState extends Equatable {
  const NotBillingsListState();
  @override
  List<Object?> get props => [];
}

final class NotBillingsListInitial extends NotBillingsListState {
  const NotBillingsListInitial();
}

final class NotBillingsListLoading extends NotBillingsListState {
  const NotBillingsListLoading();
}

final class NotBillingsListLoaded extends NotBillingsListState {
  final List<NotBilling> records;
  final int pendingOrFailedCount;

  const NotBillingsListLoaded({
    required this.records,
    required this.pendingOrFailedCount,
  });

  NotBillingsListLoaded copyWith({
    List<NotBilling>? records,
    int? pendingOrFailedCount,
  }) =>
      NotBillingsListLoaded(
        records: records ?? this.records,
        pendingOrFailedCount: pendingOrFailedCount ?? this.pendingOrFailedCount,
      );

  @override
  List<Object?> get props => [records, pendingOrFailedCount];
}

final class NotBillingsListError extends NotBillingsListState {
  final String message;
  const NotBillingsListError(this.message);
  @override
  List<Object?> get props => [message];
}
