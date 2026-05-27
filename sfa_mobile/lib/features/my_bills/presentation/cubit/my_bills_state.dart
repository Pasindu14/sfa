import 'package:uswatte/features/my_bills/domain/entities/my_bill_summary.dart';

sealed class MyBillsState {
  const MyBillsState();
}

class MyBillsInitial extends MyBillsState {
  const MyBillsInitial();
}

class MyBillsLoading extends MyBillsState {
  const MyBillsLoading();
}

class MyBillsLoaded extends MyBillsState {
  final List<MyBillSummary> bills;
  final bool hasMore;
  final bool isLoadingMore;

  const MyBillsLoaded(
    this.bills, {
    this.hasMore = false,
    this.isLoadingMore = false,
  });
}

class MyBillsError extends MyBillsState {
  final String message;
  const MyBillsError(this.message);
}
