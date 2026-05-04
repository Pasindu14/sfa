import 'package:equatable/equatable.dart';
import 'package:uswatte/features/route_assignment/domain/entities/rep_summary.dart';

abstract class SupervisorBillingEvent extends Equatable {
  const SupervisorBillingEvent();

  @override
  List<Object?> get props => [];
}

class LoadRepsRequested extends SupervisorBillingEvent {
  const LoadRepsRequested();
}

class RepSelected extends SupervisorBillingEvent {
  final RepSummary rep;
  const RepSelected(this.rep);

  @override
  List<Object?> get props => [rep];
}

class DateSelected extends SupervisorBillingEvent {
  final DateTime date;
  const DateSelected(this.date);

  @override
  List<Object?> get props => [date];
}

class LoadBillingsRequested extends SupervisorBillingEvent {
  const LoadBillingsRequested();
}
