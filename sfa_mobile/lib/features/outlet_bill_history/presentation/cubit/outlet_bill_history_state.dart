import 'package:uswatte/features/outlet_bill_history/domain/entities/outlet_bill_summary.dart';

sealed class OutletBillHistoryState {
  const OutletBillHistoryState();
}

class OutletBillHistoryInitial extends OutletBillHistoryState {
  const OutletBillHistoryInitial();
}

class OutletBillHistoryLoading extends OutletBillHistoryState {
  const OutletBillHistoryLoading();
}

class OutletBillHistoryLoaded extends OutletBillHistoryState {
  final List<OutletBillSummary> bills;
  final bool hasMore;
  final bool isLoadingMore;

  const OutletBillHistoryLoaded(
    this.bills, {
    this.hasMore = false,
    this.isLoadingMore = false,
  });
}

class OutletBillHistoryError extends OutletBillHistoryState {
  final String message;
  const OutletBillHistoryError(this.message);
}
