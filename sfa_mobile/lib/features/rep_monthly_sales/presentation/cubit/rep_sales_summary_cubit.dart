import 'package:dio/dio.dart';
import 'package:flutter/material.dart' show DateTimeRange;
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:uswatte/features/rep_monthly_sales/data/datasources/rep_billing_summary_remote_datasource.dart';
import 'package:uswatte/features/supervisor_sales_summary/domain/entities/rep_billing_summary.dart';

class RepSalesSummaryState {
  final DateTimeRange range;
  final bool isLoading;
  final RepBillingSummary? summary;
  final String? error;

  const RepSalesSummaryState({
    required this.range,
    this.isLoading = false,
    this.summary,
    this.error,
  });

  RepSalesSummaryState copyWith({
    DateTimeRange? range,
    bool? isLoading,
    RepBillingSummary? summary,
    bool clearSummary = false,
    String? error,
    bool clearError = false,
  }) {
    return RepSalesSummaryState(
      range: range ?? this.range,
      isLoading: isLoading ?? this.isLoading,
      summary: clearSummary ? null : (summary ?? this.summary),
      error: clearError ? null : (error ?? this.error),
    );
  }
}

/// The calling rep's own Sales Summary page: the rep is implicit (the server
/// scopes the request to the JWT user), so only the date range is chosen.
class RepSalesSummaryCubit extends Cubit<RepSalesSummaryState> {
  final RepBillingSummaryRemoteDatasource _remote;

  RepSalesSummaryCubit(this._remote, {DateTime Function()? clock})
      : super(RepSalesSummaryState(range: _today((clock ?? DateTime.now)())));

  /// Today only — the default view; the rep widens it from the filter.
  static DateTimeRange _today(DateTime now) {
    final day = DateTime(now.year, now.month, now.day);
    return DateTimeRange(start: day, end: day);
  }

  void selectRange(DateTimeRange range) =>
      emit(state.copyWith(range: range, clearSummary: true, clearError: true));

  Future<void> load() async {
    if (state.isLoading) return;
    final range = state.range;
    emit(state.copyWith(isLoading: true, clearSummary: true, clearError: true));
    try {
      final summary = await _remote.getMyBillingSummary(range.start, range.end);
      emit(state.copyWith(isLoading: false, summary: summary));
    } catch (e) {
      emit(state.copyWith(isLoading: false, error: _message(e)));
    }
  }

  static String _message(Object e) {
    if (e is DioException) {
      final data = e.response?.data;
      if (data is Map && data['message'] is String) {
        final fields = data['fields'];
        if (fields is Map && fields.isNotEmpty) {
          final first = fields.values.first;
          if (first is List && first.isNotEmpty) return first.first.toString();
        }
        return data['message'] as String;
      }
    }
    return 'Failed to load the summary. Please try again.';
  }
}
