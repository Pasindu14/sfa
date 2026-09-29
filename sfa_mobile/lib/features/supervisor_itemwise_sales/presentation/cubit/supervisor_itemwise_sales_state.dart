import 'package:flutter/material.dart' show DateTimeRange;
import 'package:uswatte/features/route_assignment/domain/entities/rep_summary.dart';
import 'package:uswatte/features/supervisor_itemwise_sales/domain/entities/rep_itemwise_sales.dart';

sealed class SupervisorItemwiseSalesState {
  const SupervisorItemwiseSalesState();
}

class ItemwiseSalesLoadingReps extends SupervisorItemwiseSalesState {
  const ItemwiseSalesLoadingReps();
}

class ItemwiseSalesRepsError extends SupervisorItemwiseSalesState {
  final String message;
  const ItemwiseSalesRepsError(this.message);
}

class ItemwiseSalesReady extends SupervisorItemwiseSalesState {
  final List<RepSummary> reps;
  final RepSummary? selectedRep;
  final DateTimeRange range;
  final bool isLoading;
  final RepItemwiseSales? sales;
  final String? error;

  const ItemwiseSalesReady({
    required this.reps,
    required this.range,
    this.selectedRep,
    this.isLoading = false,
    this.sales,
    this.error,
  });

  bool get canLoad => selectedRep != null && !isLoading;

  ItemwiseSalesReady copyWith({
    RepSummary? selectedRep,
    DateTimeRange? range,
    bool? isLoading,
    RepItemwiseSales? sales,
    bool clearSales = false,
    String? error,
    bool clearError = false,
  }) {
    return ItemwiseSalesReady(
      reps: reps,
      selectedRep: selectedRep ?? this.selectedRep,
      range: range ?? this.range,
      isLoading: isLoading ?? this.isLoading,
      sales: clearSales ? null : (sales ?? this.sales),
      error: clearError ? null : (error ?? this.error),
    );
  }
}
