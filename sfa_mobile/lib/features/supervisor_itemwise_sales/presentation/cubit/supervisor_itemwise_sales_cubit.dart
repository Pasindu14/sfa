import 'package:dio/dio.dart';
import 'package:flutter/material.dart' show DateTimeRange;
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:uswatte/features/route_assignment/domain/entities/rep_summary.dart';
import 'package:uswatte/features/route_assignment/domain/usecases/get_my_reps_usecase.dart';
import 'package:uswatte/features/supervisor_itemwise_sales/data/datasources/supervisor_itemwise_sales_remote_datasource.dart';
import 'package:uswatte/features/supervisor_itemwise_sales/presentation/cubit/supervisor_itemwise_sales_state.dart';

class SupervisorItemwiseSalesCubit extends Cubit<SupervisorItemwiseSalesState> {
  final GetMyRepsUseCase _getMyReps;
  final SupervisorItemwiseSalesRemoteDatasource _remote;
  final DateTime Function() _now;

  SupervisorItemwiseSalesCubit({
    required GetMyRepsUseCase getMyReps,
    required SupervisorItemwiseSalesRemoteDatasource remote,
    DateTime Function()? clock,
  }) : _getMyReps = getMyReps,
       _remote = remote,
       _now = clock ?? DateTime.now,
       super(const ItemwiseSalesLoadingReps());

  /// This month so far — same default as the Sales Summary.
  DateTimeRange get defaultRange {
    final now = _now();
    final today = DateTime(now.year, now.month, now.day);
    return DateTimeRange(start: DateTime(now.year, now.month, 1), end: today);
  }

  Future<void> loadReps() async {
    emit(const ItemwiseSalesLoadingReps());
    try {
      final reps = await _getMyReps();
      emit(ItemwiseSalesReady(reps: reps, range: defaultRange));
    } catch (_) {
      emit(
        const ItemwiseSalesRepsError('Failed to load reps. Please try again.'),
      );
    }
  }

  void selectRep(RepSummary rep) {
    final s = state;
    if (s is! ItemwiseSalesReady) return;
    emit(s.copyWith(selectedRep: rep, clearSales: true, clearError: true));
  }

  void selectRange(DateTimeRange range) {
    final s = state;
    if (s is! ItemwiseSalesReady) return;
    emit(s.copyWith(range: range, clearSales: true, clearError: true));
  }

  Future<void> load() async {
    final s = state;
    if (s is! ItemwiseSalesReady || !s.canLoad) return;
    emit(s.copyWith(isLoading: true, clearSales: true, clearError: true));
    try {
      final sales = await _remote.getRepItemwiseSales(
        s.selectedRep!.userId,
        s.range.start,
        s.range.end,
      );
      final cur = state;
      if (cur is! ItemwiseSalesReady) return;
      emit(cur.copyWith(isLoading: false, sales: sales));
    } catch (e) {
      final cur = state;
      if (cur is! ItemwiseSalesReady) return;
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
    return 'Failed to load item-wise sales. Please try again.';
  }
}
