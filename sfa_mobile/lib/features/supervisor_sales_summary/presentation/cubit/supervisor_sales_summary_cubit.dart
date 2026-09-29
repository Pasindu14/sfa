import 'package:dio/dio.dart';
import 'package:flutter/material.dart' show DateTimeRange;
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:uswatte/features/route_assignment/domain/entities/rep_summary.dart';
import 'package:uswatte/features/route_assignment/domain/usecases/get_my_reps_usecase.dart';
import 'package:uswatte/features/supervisor_sales_summary/data/datasources/supervisor_sales_summary_remote_datasource.dart';
import 'package:uswatte/features/supervisor_sales_summary/presentation/cubit/supervisor_sales_summary_state.dart';

class SupervisorSalesSummaryCubit extends Cubit<SupervisorSalesSummaryState> {
  final GetMyRepsUseCase _getMyReps;
  final SupervisorSalesSummaryRemoteDatasource _remote;
  final DateTime Function() _now;

  SupervisorSalesSummaryCubit({
    required GetMyRepsUseCase getMyReps,
    required SupervisorSalesSummaryRemoteDatasource remote,
    DateTime Function()? clock,
  })  : _getMyReps = getMyReps,
        _remote = remote,
        _now = clock ?? DateTime.now,
        super(const SalesSummaryLoadingReps());

  /// This month so far — the most common question a supervisor asks.
  DateTimeRange get defaultRange {
    final now = _now();
    final today = DateTime(now.year, now.month, now.day);
    return DateTimeRange(start: DateTime(now.year, now.month, 1), end: today);
  }

  Future<void> loadReps() async {
    emit(const SalesSummaryLoadingReps());
    try {
      final reps = await _getMyReps();
      emit(SalesSummaryReady(reps: reps, range: defaultRange));
    } catch (_) {
      emit(const SalesSummaryRepsError('Failed to load reps. Please try again.'));
    }
  }

  void selectRep(RepSummary rep) {
    final s = state;
    if (s is! SalesSummaryReady) return;
    emit(s.copyWith(selectedRep: rep, clearSummary: true, clearError: true));
  }

  void selectRange(DateTimeRange range) {
    final s = state;
    if (s is! SalesSummaryReady) return;
    emit(s.copyWith(range: range, clearSummary: true, clearError: true));
  }

  Future<void> load() async {
    final s = state;
    if (s is! SalesSummaryReady || !s.canLoad) return;
    emit(s.copyWith(isLoading: true, clearSummary: true, clearError: true));
    try {
      final summary = await _remote.getRepBillingSummary(
          s.selectedRep!.userId, s.range.start, s.range.end);
      final cur = state;
      if (cur is! SalesSummaryReady) return;
      emit(cur.copyWith(isLoading: false, summary: summary));
    } catch (e) {
      final cur = state;
      if (cur is! SalesSummaryReady) return;
      emit(cur.copyWith(isLoading: false, error: _message(e)));
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
