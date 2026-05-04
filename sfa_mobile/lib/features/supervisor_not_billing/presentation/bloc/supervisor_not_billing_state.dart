import 'package:equatable/equatable.dart';
import 'package:uswatte/features/route_assignment/domain/entities/rep_summary.dart';
import 'package:uswatte/features/supervisor_not_billing/domain/entities/not_billing_summary.dart';

abstract class SupervisorNotBillingState extends Equatable {
  const SupervisorNotBillingState();

  @override
  List<Object?> get props => [];
}

class SupervisorNotBillingInitial extends SupervisorNotBillingState {
  const SupervisorNotBillingInitial();
}

class SupervisorNotBillingLoading extends SupervisorNotBillingState {
  const SupervisorNotBillingLoading();
}

class SupervisorNotBillingError extends SupervisorNotBillingState {
  final String message;
  const SupervisorNotBillingError(this.message);

  @override
  List<Object?> get props => [message];
}

class SupervisorNotBillingReady extends SupervisorNotBillingState {
  final List<RepSummary> reps;
  final RepSummary? selectedRep;
  final DateTime selectedDate;
  final List<NotBillingSummary>? notBillings;
  final bool isLoadingNotBillings;
  final String? notBillingsError;

  const SupervisorNotBillingReady({
    required this.reps,
    this.selectedRep,
    required this.selectedDate,
    this.notBillings,
    this.isLoadingNotBillings = false,
    this.notBillingsError,
  });

  bool get canLoad => selectedRep != null && !isLoadingNotBillings;
  bool get hasResults => notBillings != null;

  SupervisorNotBillingReady copyWith({
    List<RepSummary>? reps,
    RepSummary? selectedRep,
    bool clearRep = false,
    DateTime? selectedDate,
    List<NotBillingSummary>? notBillings,
    bool clearNotBillings = false,
    bool? isLoadingNotBillings,
    String? notBillingsError,
    bool clearNotBillingsError = false,
  }) {
    return SupervisorNotBillingReady(
      reps: reps ?? this.reps,
      selectedRep: clearRep ? null : (selectedRep ?? this.selectedRep),
      selectedDate: selectedDate ?? this.selectedDate,
      notBillings:
          clearNotBillings ? null : (notBillings ?? this.notBillings),
      isLoadingNotBillings:
          isLoadingNotBillings ?? this.isLoadingNotBillings,
      notBillingsError: clearNotBillingsError
          ? null
          : (notBillingsError ?? this.notBillingsError),
    );
  }

  @override
  List<Object?> get props => [
        reps,
        selectedRep,
        selectedDate,
        notBillings,
        isLoadingNotBillings,
        notBillingsError,
      ];
}
