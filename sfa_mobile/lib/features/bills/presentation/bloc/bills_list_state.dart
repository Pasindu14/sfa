import 'package:equatable/equatable.dart';
import 'package:uswatte/features/bills/domain/entities/bill.dart';

sealed class BillsListState extends Equatable {
  const BillsListState();
  @override
  List<Object?> get props => [];
}

final class BillsListInitial extends BillsListState {
  const BillsListInitial();
}

final class BillsListLoading extends BillsListState {
  const BillsListLoading();
}

final class BillsListLoaded extends BillsListState {
  final List<Bill> bills;
  final int pendingOrFailedCount;

  const BillsListLoaded({
    required this.bills,
    required this.pendingOrFailedCount,
  });

  BillsListLoaded copyWith({
    List<Bill>? bills,
    int? pendingOrFailedCount,
  }) =>
      BillsListLoaded(
        bills: bills ?? this.bills,
        pendingOrFailedCount:
            pendingOrFailedCount ?? this.pendingOrFailedCount,
      );

  @override
  List<Object?> get props => [bills, pendingOrFailedCount];
}

final class BillsListError extends BillsListState {
  final String message;
  const BillsListError(this.message);
  @override
  List<Object?> get props => [message];
}
