import 'package:equatable/equatable.dart';
import 'package:uswatte/features/route_assignment/domain/entities/rep_summary.dart';
import 'package:uswatte/features/supervisor_billing/domain/entities/billing_summary.dart';

abstract class SupervisorBillingState extends Equatable {
  const SupervisorBillingState();

  @override
  List<Object?> get props => [];
}

class SupervisorBillingInitial extends SupervisorBillingState {
  const SupervisorBillingInitial();
}

class SupervisorBillingLoadingReps extends SupervisorBillingState {
  const SupervisorBillingLoadingReps();
}

class SupervisorBillingLoadError extends SupervisorBillingState {
  final String message;
  const SupervisorBillingLoadError(this.message);

  @override
  List<Object?> get props => [message];
}

class SupervisorBillingReady extends SupervisorBillingState {
  final List<RepSummary> reps;
  final RepSummary? selectedRep;
  final DateTime selectedDate;
  final List<BillingSummary>? billings;
  final bool isLoadingBillings;
  final String? billingsError;

  const SupervisorBillingReady({
    required this.reps,
    this.selectedRep,
    required this.selectedDate,
    this.billings,
    this.isLoadingBillings = false,
    this.billingsError,
  });

  bool get canLoad => selectedRep != null && !isLoadingBillings;
  bool get hasResults => billings != null;

  SupervisorBillingReady copyWith({
    List<RepSummary>? reps,
    RepSummary? selectedRep,
    bool clearRep = false,
    DateTime? selectedDate,
    List<BillingSummary>? billings,
    bool clearBillings = false,
    bool? isLoadingBillings,
    String? billingsError,
    bool clearBillingsError = false,
  }) {
    return SupervisorBillingReady(
      reps: reps ?? this.reps,
      selectedRep: clearRep ? null : (selectedRep ?? this.selectedRep),
      selectedDate: selectedDate ?? this.selectedDate,
      billings: clearBillings ? null : (billings ?? this.billings),
      isLoadingBillings: isLoadingBillings ?? this.isLoadingBillings,
      billingsError:
          clearBillingsError ? null : (billingsError ?? this.billingsError),
    );
  }

  @override
  List<Object?> get props => [
        reps,
        selectedRep,
        selectedDate,
        billings,
        isLoadingBillings,
        billingsError,
      ];
}
