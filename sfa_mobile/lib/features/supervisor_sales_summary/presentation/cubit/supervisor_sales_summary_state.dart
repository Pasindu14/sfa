import 'package:flutter/material.dart' show DateTimeRange;
import 'package:uswatte/features/route_assignment/domain/entities/rep_summary.dart';
import 'package:uswatte/features/supervisor_sales_summary/domain/entities/rep_billing_summary.dart';

sealed class SupervisorSalesSummaryState {
  const SupervisorSalesSummaryState();
}

class SalesSummaryLoadingReps extends SupervisorSalesSummaryState {
  const SalesSummaryLoadingReps();
}

class SalesSummaryRepsError extends SupervisorSalesSummaryState {
  final String message;
  const SalesSummaryRepsError(this.message);
}

class SalesSummaryReady extends SupervisorSalesSummaryState {
  final List<RepSummary> reps;
  final RepSummary? selectedRep;
  final DateTimeRange range;
  final bool isLoading;
  final RepBillingSummary? summary;
  final String? error;

  const SalesSummaryReady({
    required this.reps,
    required this.range,
    this.selectedRep,
    this.isLoading = false,
    this.summary,
    this.error,
  });

  bool get canLoad => selectedRep != null && !isLoading;

  SalesSummaryReady copyWith({
    RepSummary? selectedRep,
    DateTimeRange? range,
    bool? isLoading,
    RepBillingSummary? summary,
    bool clearSummary = false,
    String? error,
    bool clearError = false,
  }) {
    return SalesSummaryReady(
      reps: reps,
      selectedRep: selectedRep ?? this.selectedRep,
      range: range ?? this.range,
      isLoading: isLoading ?? this.isLoading,
      summary: clearSummary ? null : (summary ?? this.summary),
      error: clearError ? null : (error ?? this.error),
    );
  }
}
