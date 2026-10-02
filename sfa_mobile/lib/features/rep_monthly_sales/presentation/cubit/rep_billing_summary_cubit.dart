import 'package:equatable/equatable.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:uswatte/features/rep_monthly_sales/data/datasources/rep_billing_summary_remote_datasource.dart';
import 'package:uswatte/features/supervisor_sales_summary/domain/entities/rep_billing_summary.dart';

sealed class RepBillingSummaryState extends Equatable {
  const RepBillingSummaryState();

  @override
  List<Object?> get props => [];
}

class RepBillingSummaryInitial extends RepBillingSummaryState {
  const RepBillingSummaryInitial();
}

class RepBillingSummaryLoading extends RepBillingSummaryState {
  const RepBillingSummaryLoading();
}

class RepBillingSummaryLoaded extends RepBillingSummaryState {
  final RepBillingSummary summary;
  const RepBillingSummaryLoaded(this.summary);

  @override
  List<Object?> get props => [summary];
}

class RepBillingSummaryError extends RepBillingSummaryState {
  const RepBillingSummaryError();
}

/// The rep's own month-to-date billing summary for the home page cards.
class RepBillingSummaryCubit extends Cubit<RepBillingSummaryState> {
  final RepBillingSummaryRemoteDatasource _remote;
  final DateTime Function() _now;

  RepBillingSummaryCubit(this._remote, {DateTime Function()? clock})
      : _now = clock ?? DateTime.now,
        super(const RepBillingSummaryInitial());

  /// 1st of the current month → today.
  Future<void> load() async {
    // Keep the previous figures on screen during a pull-to-refresh.
    if (state is! RepBillingSummaryLoaded) {
      emit(const RepBillingSummaryLoading());
    }
    try {
      final now = _now();
      final summary = await _remote.getMyBillingSummary(
        DateTime(now.year, now.month, 1),
        DateTime(now.year, now.month, now.day),
      );
      emit(RepBillingSummaryLoaded(summary));
    } catch (_) {
      if (state is! RepBillingSummaryLoaded) {
        emit(const RepBillingSummaryError());
      }
    }
  }
}
